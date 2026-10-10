using System.Security.Cryptography;
using System.Text.Json;
using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class DatasetSamplingTests
{
    public static async Task RunAsync(DatasetStore source, string artifacts)
    {
        var store = new DatasetStore(Path.Combine(artifacts, "sampled-evaluation-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(store.DirectoryPath);
        var original = source.LoadManifest();
        var originalFrame = original.Frames[^1];
        var manifest = new DatasetManifest { Recognition = ConfigStore.RecognitionSnapshot(original.Recognition) };
        manifest.Recognition.RollingIntervalMs = 3000;
        var offsets = new double[] { 0, 100, 200, 1_000_000 };
        for (var i = 0; i < offsets.Length; i++)
        {
            var file = Path.Combine(store.DirectoryPath, $"{i}.png");
            if (i is 0 or 3) File.Copy(source.FramePath(originalFrame), file);
            else File.WriteAllText(file, "unselected invalid image");
            using var stream = File.OpenRead(file);
            var hash = i is 0 or 3 ? Convert.ToHexString(SHA256.HashData(stream)) : "invalid-skipped-hash";
            manifest.Frames.Add(originalFrame with { Index = (i + 1) * 20, File = $"{i}.png", OffsetMs = offsets[i], Sha256 = hash });
        }
        store.SaveManifest(manifest);
        var truth = source.LoadAnnotations().Frames[originalFrame.Index];
        store.SaveAnnotations(new() { Frames = new() { [20] = truth, [80] = truth } });
        var messages = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var directory = await DatasetEvaluator.EvaluateAsync(store, true, new TestProgress(messages.Enqueue),
            CancellationToken.None, workerCount: 1);
        var report = ReadReport(directory);
        Program.Assert(report.Mode == "production-timing" && report.TotalFrames == 4, "报告须保留录制总数和采样模式");
        Program.Assert(report.Result.Frames.Select(frame => frame.Index).SequenceEqual(new[] { 20, 80 }),
            "只能读取和识别采样帧，期间无效图片不能被解码或校验");
        Program.Assert(report.Result.Frames.All(frame => frame.Candidates.Count == Ocr.RollingOcrRecognizer.CandidateCount),
            "必须执行当前正式方案的全部候选 OCR");
        Program.Assert(messages.Any(message => message.Contains("已识别 2 个样本")), "进度须显示已识别样本数");
        messages.Clear();
        var parallelDirectory = await DatasetEvaluator.EvaluateAsync(store, true, new TestProgress(messages.Enqueue),
            CancellationToken.None, workerCount: 2);
        var parallelReport = ReadReport(parallelDirectory);
        Program.Assert(report.ExecutionMode == "single-segment" && parallelReport.ExecutionMode == "parallel-segments"
            && parallelReport.WorkerCount == 2 && parallelReport.WallMilliseconds > 0, "报告必须记录分段线程数和总耗时");
        Program.Assert(CandidateText(report).SequenceEqual(CandidateText(parallelReport)), "并行分段须对稀疏样本生成相同真实候选文本");
        Program.Assert(JsonSerializer.Serialize(report.Result.Strict) == JsonSerializer.Serialize(parallelReport.Result.Strict),
            "相同样本的单线程和多线程文本准确度须一致");
        Program.Assert(EventText(report).SequenceEqual(EventText(parallelReport))
            && parallelReport.Result.Events.Count > 0, "跨片段重复画面须统一去重，最终事件应与连续回放一致");
        Program.Assert(messages.Any(message => message.Contains("已结束 2/2 段")), "进度须显示各片段已完成状态");
        Program.Assert(File.ReadAllText(Path.Combine(parallelDirectory, "report.md")).Contains("不能直接代表正式单独运行的性能"),
            "报告须明确并行耗时口径");
        Console.WriteLine($"Current OCR comparison: 1 worker {report.WallMilliseconds:F0}ms, 2 workers {parallelReport.WallMilliseconds:F0}ms");
        await ExpectCorruptionAsync(store, false, 1);
        await ExpectCorruptionAsync(store, false, 2);
        await File.AppendAllTextAsync(store.FramePath(manifest.Frames[0]), "changed sampled input");
        await ExpectCorruptionAsync(store, true, 1);
        await ExpectCorruptionAsync(store, true, 2);
        Console.WriteLine("PASS: current OCR, sampled input integrity, parallel segments, accuracy equivalence, cross-segment deduplication");
    }

    private static EvaluationReport ReadReport(string directory)
        => JsonSerializer.Deserialize<EvaluationReport>(File.ReadAllText(Path.Combine(directory, "report.json")))!;
    private static IEnumerable<string> CandidateText(EvaluationReport report)
        => report.Result.Frames.SelectMany(frame => frame.Candidates.Select(candidate => $"{frame.Index}/{candidate.Name}/{candidate.Text}"));
    private static IEnumerable<string> EventText(EvaluationReport report)
        => report.Result.Events.Select(item => $"{item.TimeText}/{item.Content}");

    private static async Task ExpectCorruptionAsync(DatasetStore store, bool productionTiming, int workers)
    {
        try { await DatasetEvaluator.EvaluateAsync(store, productionTiming, null, CancellationToken.None, workers); }
        catch (InvalidDataException ex) when (ex.Message.StartsWith("图片校验失败")) { return; }
        throw new InvalidOperationException("实际参与评估的损坏输入必须被拒绝");
    }
}
