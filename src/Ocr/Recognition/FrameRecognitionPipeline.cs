using System.Diagnostics;
using System.Drawing;
using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Ocr;

// Live processing calls all three stages in sequence. Evaluation prepares each segment
// in order on its worker, then commits all recognized results in global chronological order.
internal sealed class FrameRecognitionPipeline(Arguments config, bool debug = false)
{
    private readonly RollingOcrOutputFilter _filter = new();
    private Rectangle? _crop;
    private Size _size;
    public bool Initialized => _crop.HasValue;
    public Rectangle? Crop => _crop;

    public async Task<FrameRecognition> ProcessAsync(Bitmap screenshot, CancellationToken token)
    {
        var prepared = await PrepareAsync(screenshot, token);
        var recognized = await RecognizePreparedAsync(screenshot, prepared, config, debug, token);
        return Commit(recognized);
    }

    public async Task<PreparedFrame> PrepareAsync(Bitmap screenshot, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            token.ThrowIfCancellationRequested();
            if (_size != screenshot.Size) { _size = screenshot.Size; _crop = null; }
            if (!_crop.HasValue)
            {
                var initial = await OcrEngine.RecognizeWithWordsAsync(screenshot, config.Language, 4,
                    OcrEngine.OcrPreprocessMode.HighContrastBinary, debug);
                Infrastructure.Logger.Debug($"初次 OCR 原文：{initial.Text}");
                token.ThrowIfCancellationRequested();
                var anchor = OcrEngine.FindTextRect(initial.Words, config.SearchText, config.CaseSensitive);
                if (anchor == null || anchor.Value.Bottom >= screenshot.Height)
                    return new(null, stopwatch.Elapsed.TotalMilliseconds, "未找到可用的日志锚点");
                _crop = new Rectangle(anchor.Value.Left, anchor.Value.Bottom,
                    screenshot.Width - anchor.Value.Left, screenshot.Height - anchor.Value.Bottom);
            }
            return new(_crop, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return new(_crop, stopwatch.Elapsed.TotalMilliseconds, ex.Message); }
    }

    public static async Task<FrameRecognition> RecognizePreparedAsync(Bitmap screenshot, PreparedFrame prepared,
        Arguments config, bool debug, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            token.ThrowIfCancellationRequested();
            if (prepared.Error != null || prepared.Crop == null)
                return new(prepared.Crop.HasValue, null, null, prepared.Milliseconds, prepared.Error, prepared.Crop);
            using var cropped = screenshot.Clone(prepared.Crop.Value, screenshot.PixelFormat);
            var recognition = await RollingOcrRecognizer.RecognizeRollingAsync(cropped, config.Language, debug);
            token.ThrowIfCancellationRequested();
            return new(true, recognition, null, prepared.Milliseconds + stopwatch.Elapsed.TotalMilliseconds, Crop: prepared.Crop);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return new(prepared.Crop.HasValue, null, null,
                prepared.Milliseconds + stopwatch.Elapsed.TotalMilliseconds, ex.Message, prepared.Crop);
        }
    }

    public FrameRecognition Commit(FrameRecognition result)
    {
        if (result.Recognition == null) return result;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var filtered = _filter.Filter(result.Recognition.Selected.Result.Lines);
            return result with { Filter = filtered, Error = filtered.RejectionReason,
                TotalMilliseconds = result.TotalMilliseconds + stopwatch.Elapsed.TotalMilliseconds };
        }
        catch (Exception ex)
        {
            return result with { Recognition = null, Filter = null, Error = ex.Message,
                TotalMilliseconds = result.TotalMilliseconds + stopwatch.Elapsed.TotalMilliseconds };
        }
    }

    public static int NextDelay(FrameRecognition result, Arguments config)
        => result.AnchorFound ? config.RollingIntervalMs : config.RetryInterval;
}
