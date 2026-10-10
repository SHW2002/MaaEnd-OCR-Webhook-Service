using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class EvaluationReplay
{
    public static async Task<EvaluationResult> RunAsync(DatasetStore store, DatasetManifest manifest,
        bool sampleFrames, IReadOnlyList<ReplaySegment> segments, IProgress<string>? progress, CancellationToken token)
    {
        var updates = new EvaluationProgress(segments, progress);
        var runs = await SegmentExecutor.RunAsync(segments,
            (segment, runToken) => RunSegmentAsync(store, manifest, sampleFrames, segment, updates, runToken), token);
        progress?.Report("识别完成，正在按时间顺序过滤、去重并计算指标…");
        var result = new EvaluationResult();
        var pipeline = new FrameRecognitionPipeline(manifest.Recognition);
        foreach (var run in runs)
        {
            result.Segments.Add(new(run.Segment.Index + 1, run.Segment.StartMs, run.Segment.EndMs,
                run.Segment.EndPosition - run.Segment.StartPosition, run.Samples.Count, run.WallMilliseconds, run.StopReason));
            foreach (var sample in run.Samples)
            {
                token.ThrowIfCancellationRequested();
                var recognized = pipeline.Commit(sample.Recognition);
                var frame = sample.Frame;
                var crop = recognized.Crop;
                var candidates = recognized.Recognition?.Candidates.Select(x =>
                    new CandidateEvaluation(x.Spec.Name, x.Score?.Value, x.ElapsedMilliseconds, x.Result.Text)).ToList() ?? [];
                result.Frames.Add(new(frame.Index, frame.OffsetMs, recognized.AnchorFound, recognized.Accepted,
                    recognized.Recognition?.Selected.Spec.Name, recognized.TotalMilliseconds, recognized.Error,
                    recognized.Lines.Select(x => new TruthLine(x.TimeText, x.Content)).ToList(), candidates,
                    crop?.X, crop?.Y, crop?.Width, crop?.Height));
                foreach (var line in recognized.NewLines)
                    result.Events.Add(new(line.TimeText, line.Content, frame.OffsetMs + recognized.TotalMilliseconds, frame.OffsetMs));
            }
        }
        return result;
    }

    private static async Task<SegmentRun> RunSegmentAsync(DatasetStore store, DatasetManifest manifest,
        bool sampleFrames, ReplaySegment segment, EvaluationProgress progress, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var pipeline = new FrameRecognitionPipeline(manifest.Recognition);
        var samples = new List<RecognizedSample>();
        var position = segment.StartPosition;
        var failures = 0;
        string? stopReason = null;
        while (position < segment.EndPosition)
        {
            token.ThrowIfCancellationRequested();
            var frame = manifest.Frames[position];
            using var stream = File.OpenRead(store.FramePath(frame));
            if (Convert.ToHexString(SHA256.HashData(stream)) != frame.Sha256)
                throw new InvalidDataException($"图片校验失败：{frame.File}");
            token.ThrowIfCancellationRequested();
            stream.Position = 0;
            using var bitmap = new Bitmap(stream);
            var prepared = await pipeline.PrepareAsync(bitmap, token);
            var recognized = await FrameRecognitionPipeline.RecognizePreparedAsync(bitmap, prepared,
                manifest.Recognition, false, token);
            samples.Add(new(frame, recognized));
            progress.Update(segment.Index, samples.Count, frame.OffsetMs);
            position = ReplaySampler.NextPosition(manifest.Frames, position, recognized.TotalMilliseconds,
                FrameRecognitionPipeline.NextDelay(recognized, manifest.Recognition), sampleFrames);
            if (recognized.AnchorFound) failures = 0;
            else if (++failures >= manifest.Recognition.Retry && sampleFrames)
            { stopReason = "达到本段的锚点重试上限"; break; }
        }
        token.ThrowIfCancellationRequested();
        progress.Update(segment.Index, samples.Count,
            stopReason == null ? segment.EndMs : samples[^1].Frame.OffsetMs, true);
        return new(segment, samples, stopwatch.Elapsed.TotalMilliseconds, stopReason);
    }

    private sealed record RecognizedSample(DatasetFrame Frame, FrameRecognition Recognition);
    private sealed record SegmentRun(ReplaySegment Segment, List<RecognizedSample> Samples,
        double WallMilliseconds, string? StopReason);
}
