using System.Diagnostics;
using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class DatasetEvaluator
{
    public static async Task<string> EvaluateAsync(DatasetStore store, bool productionTiming,
        IProgress<string>? progress, CancellationToken token, int workerCount = 0)
    {
        var stopwatch = Stopwatch.StartNew();
        token.ThrowIfCancellationRequested();
        if (workerCount == 0) workerCount = ReplaySegments.DefaultWorkers;
        progress?.Report("正在读取数据集清单和标注…");
        var manifest = store.LoadManifest();
        var annotations = store.LoadAnnotations();
        if (!manifest.Recognition.TryValidate(true, out var errors))
            throw new InvalidDataException(string.Join("\n", errors));
        var segments = ReplaySegments.Split(manifest.Frames, workerCount);
        var report = new EvaluationReport
        {
            SessionId = manifest.SessionId, Mode = productionTiming ? "production-timing" : "all-frames",
            ExecutionMode = segments.Count > 1 ? "parallel-segments" : "single-segment",
            RequestedWorkers = workerCount, WorkerCount = segments.Count,
            Recognition = ConfigStore.RecognitionSnapshot(manifest.Recognition), TotalFrames = manifest.Frames.Count,
            ReviewedFrames = manifest.Frames.Count(x => annotations.Frames.GetValueOrDefault(x.Index)?.Reviewed == true),
            AvailableOcrLanguages = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages.Select(x => x.LanguageTag).ToArray()
        };
        progress?.Report($"当前方案：{segments.Count} 个时间片段并行评估"
            + (productionTiming ? $"，每轮识别后按 {manifest.Recognition.RollingIntervalMs}ms 等待采样" : "，逐帧识别"));
        var run = await EvaluationReplay.RunAsync(store, manifest, productionTiming, segments, progress, token);
        token.ThrowIfCancellationRequested();
        run.Strict = AccuracyCalculator.Calculate(run.Frames, annotations, false);
        run.Normalized = AccuracyCalculator.Calculate(run.Frames, annotations, true);
        foreach (var group in run.Frames.Where(x => annotations.Frames.GetValueOrDefault(x.Index)?.Reviewed == true)
            .GroupBy(x => string.IsNullOrEmpty(annotations.Frames[x.Index].Scenario) ? "未分类" : annotations.Frames[x.Index].Scenario))
            run.Scenarios[group.Key] = AccuracyCalculator.Calculate(group.ToList(), annotations, false);
        if (annotations.EventsReviewed) run.EventMetrics = AccuracyCalculator.CalculateEvents(annotations.Events, run.Events);
        run.MedianMilliseconds = AccuracyCalculator.Percentile(run.Frames.Select(x => x.TotalMilliseconds), 0.5);
        run.P95Milliseconds = AccuracyCalculator.Percentile(run.Frames.Select(x => x.TotalMilliseconds), 0.95);
        token.ThrowIfCancellationRequested();
        report.Result = run;
        report.WallMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        var directory = Path.Combine(store.DirectoryPath, "reports", $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        DatasetStore.Write(Path.Combine(directory, "report.json"), report);
        File.WriteAllText(Path.Combine(directory, "report.md"), EvaluationReportWriter.Format(report));
        progress?.Report($"评估完成：{DatasetPaths.Display(directory)}");
        return directory;
    }
}
