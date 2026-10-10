using System.Diagnostics;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class DatasetRecorder
{
    public static async Task RecordAsync(PendingRecording recording, Arguments config, int intervalMs,
        string scenario, IProgress<string>? progress, CancellationToken token,
        IProgress<RecordingPreview>? preview = null)
    {
        if (intervalMs is < 33 or > 60000) throw new ArgumentOutOfRangeException(nameof(intervalMs));
        var window = WindowFinder.FindWindow(config.WindowTitle, config.PartialMatch, config.WindowTitleCaseSensitive)
            ?? throw new InvalidOperationException("未找到待录制窗口");
        var folder = recording.DirectoryPath;
        Directory.CreateDirectory(Path.Combine(folder, "frames"));
        var store = new DatasetStore(folder);
        var manifest = new DatasetManifest
        {
            Scenario = scenario, CaptureIntervalMs = intervalMs, Recognition = ConfigStore.RecognitionSnapshot(config),
            AvailableOcrLanguages = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages.Select(x => x.LanguageTag).ToArray()
        };
        var annotations = new DatasetAnnotations();
        manifest.StartedUtc = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        double next = 0;
        double nextPreview = 0;
        store.SaveManifest(manifest);
        store.SaveAnnotations(annotations);
        progress?.Report($"会话目录：{DatasetPaths.Display(folder)}");
        try
        {
            while (!token.IsCancellationRequested)
            {
                var remaining = next - clock.Elapsed.TotalMilliseconds;
                if (remaining > 0) await Task.Delay(TimeSpan.FromMilliseconds(remaining), token);
                var started = clock.Elapsed.TotalMilliseconds;
                var capturedUtc = DateTimeOffset.UtcNow;
                try
                {
                    using var bitmap = ScreenCapture.CaptureWindow(window, out var method);
                    var captureMs = clock.Elapsed.TotalMilliseconds - started;
                    var index = manifest.Frames.Count;
                    var relative = $"frames/{index:000000}.png";
                    var path = Path.Combine(folder, relative);
                    bitmap.Save(path, ImageFormat.Png);
                    using var imageStream = File.OpenRead(path);
                    var hash = Convert.ToHexString(SHA256.HashData(imageStream));
                    manifest.Frames.Add(new(index, relative, started, capturedUtc, bitmap.Width, bitmap.Height,
                        WindowMetadata.GetDpi(window), window.Title, captureMs, hash, method));
                    annotations.Frames[index] = new FrameAnnotation { Scenario = scenario };
                    progress?.Report($"已录制 {index + 1} 帧，{clock.Elapsed.TotalSeconds:F1} 秒，{bitmap.Width}×{bitmap.Height}");
                    if (preview != null && started >= nextPreview)
                    {
                        preview.Report(new(path, index + 1, started, bitmap.Width, bitmap.Height));
                        nextPreview = started + 500;
                    }
                }
                catch (Exception ex)
                {
                    manifest.CaptureErrors.Add(new(started, ex.Message));
                    progress?.Report($"采集失败：{ex.Message}");
                    if (manifest.CaptureErrors.Count >= 10 && manifest.Frames.Count == 0) throw;
                }
                // Persist each frame so a process crash still leaves a readable dataset.
                store.SaveManifest(manifest);
                store.SaveAnnotations(annotations);
                next += intervalMs;
                while (next < clock.Elapsed.TotalMilliseconds) { next += intervalMs; manifest.MissedCaptureSlots++; }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            manifest.FinishedUtc = DateTimeOffset.UtcNow;
            store.SaveManifest(manifest);
            store.SaveAnnotations(annotations);
        }
    }
}
