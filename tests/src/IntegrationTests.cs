using System.Text.Json;
using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Gui;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class IntegrationTests
{
    public static int Run()
    {
        var result = 1;
        using var target = new TestWindow();
        target.Shown += async (_, _) =>
        {
            try
            {
                await VerifyAsync(target);
                result = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { target.Close(); }
        };
        Application.Run(target);
        return result;
    }

    private static async Task VerifyAsync(TestWindow target)
    {
        var parent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "verification"));
        Directory.CreateDirectory(parent);
        var config = new Arguments { WindowTitle = target.Text, SearchText = "运行日志", Retry = 5, RollingIntervalMs = 500 };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var recordingProgress = new TestProgress(message =>
        {
            Console.WriteLine(message);
            if (message.StartsWith("已录制 3 帧")) cancellation.Cancel();
        });
        var previews = new List<RecordingPreview>();
        var recording = PendingRecording.Create(parent);
        await Task.Run(() => DatasetRecorder.RecordAsync(recording, config, 100,
            "自动验证 / 连续窗口", recordingProgress, cancellation.Token,
            new TestProgress<RecordingPreview>(previews.Add)));
        Program.Assert(Directory.Exists(recording.DirectoryPath) && !Directory.Exists(recording.SavedDirectory), "停止后必须保留待保存录制");
        Program.Assert(previews.Count > 0 && previews.All(x => File.Exists(x.Path)), "实时预览必须指向已写入的真实采集帧");
        var folder = recording.Save();
        var store = new DatasetStore(folder);
        var manifest = store.LoadManifest();
        Program.Assert(manifest.Frames.Count >= 2, "录制应保留连续帧");
        var capturedWindow = WindowFinder.FindWindow(target.Text, false)!;
        Program.Assert(manifest.Frames.All(x => x.Width == capturedWindow.Width && x.Height == capturedWindow.Height),
            "录制帧必须仅包含目标窗口，尺寸不能是全屏");
        Program.Assert(manifest.FinishedUtc != null && manifest.Frames.All(x => x.Dpi > 0 && x.CaptureMethod.Length > 0), "缺少完成/采集元数据");
        Program.Assert(manifest.Frames.Zip(manifest.Frames.Skip(1)).All(x => x.First.OffsetMs < x.Second.OffsetMs), "时间戳应递增");
        using (var image = new Bitmap(store.FramePath(manifest.Frames[0])))
            Program.Assert(image.Width == manifest.Frames[0].Width && image.Height == manifest.Frames[0].Height, "保存尺寸应与原始窗口一致");
        var annotations = store.LoadAnnotations();
        foreach (var frame in manifest.Frames)
            annotations.Frames[frame.Index] = new() { Reviewed = true, Lines = [new("12:00:00", "任务开始：测试任务"), new("12:00:01", "任务完成：测试任务")] };
        annotations.EventsReviewed = true;
        annotations.Events = [new("12:00:00", "任务开始：测试任务", 0), new("12:00:01", "任务完成：测试任务", 0)];
        store.SaveAnnotations(annotations);
        var directory = await Task.Run(() => DatasetEvaluator.EvaluateAsync(store, false, null, CancellationToken.None));
        var report = JsonSerializer.Deserialize<EvaluationReport>(File.ReadAllText(Path.Combine(directory, "report.json")))!;
        if (report.Result.Frames.All(f => !f.AnchorFound))
        {
            using var diagnostic = new Bitmap(store.FramePath(manifest.Frames[0]));
            var recognized = await Ocr.OcrEngine.RecognizeWithWordsAsync(diagnostic, config.Language, 4);
            Console.WriteLine("Initial OCR diagnostic: " + recognized.Text);
        }
        Program.Assert(report.Result.Frames.Count == manifest.Frames.Count, "当前方案必须处理完整逐帧输入");
        Program.Assert(report.Result.Frames.Any(f => f.AnchorFound && f.Candidates.Count == Ocr.RollingOcrRecognizer.CandidateCount),
            "必须执行正式 Windows OCR 的全部候选并定位锚点");
        Program.Assert(report.Result.Strict.CharacterErrorRate != null, "已确认真值应生成真实 CER");
        var scheduled = await Task.Run(() => DatasetEvaluator.EvaluateAsync(store, true, null, CancellationToken.None, workerCount: 1));
        var scheduledReport = JsonSerializer.Deserialize<EvaluationReport>(File.ReadAllText(Path.Combine(scheduled, "report.json")))!;
        Program.Assert(scheduledReport.Result.Frames.Count < manifest.Frames.Count, "真实节奏应依据处理时间和等待间隔跳帧");
        await DatasetSamplingTests.RunAsync(store, parent);
        var liveLogs = new List<string>();
        using var liveCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        Action<string> listener = line =>
        {
            lock (liveLogs) liveLogs.Add(line);
            if (line.Contains("[INFO] 12:00:")) liveCancellation.Cancel();
        };
        Logger.MessageLogged += listener;
        try
        {
            await Task.Run(() => LiveRecognitionRunner.RunAsync(config, false, liveCancellation.Token));
        }
        finally { Logger.MessageLogged -= listener; }
        Program.Assert(liveLogs.Any(x => x.Contains("[INFO] 12:00:")), "正式运行路径必须实际识别并输出事件");
        GuiSmoke.Run(store, parent);
        var first = store.FramePath(manifest.Frames[0]);
        await File.AppendAllTextAsync(first, "corrupted");
        try
        {
            await DatasetEvaluator.EvaluateAsync(store, false, null, CancellationToken.None);
            throw new InvalidOperationException("损坏图片不应参与评估");
        }
        catch (InvalidDataException) { }
        Console.WriteLine("PASS: actual window recording, cancellation, current OCR, segmented replay, scheduling, live pipeline, GUI, hash validation");
        Console.WriteLine("Verification artifacts: " + parent);
    }
}
