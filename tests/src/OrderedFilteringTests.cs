using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class OrderedFilteringTests
{
    public static void Run()
    {
        var pipeline = new FrameRecognitionPipeline(new Arguments());
        var visible = Raw([Line("任务开始：测试任务", "12:00:00"), Line("任务完成：测试任务", "12:00:01")]);
        Program.Assert(pipeline.Commit(visible).NewLines.Count == 2, "第一片段必须输出新日志");
        Program.Assert(pipeline.Commit(visible).NewLines.Count == 0, "第二片段重复画面也必须沿用全局去重历史");
        var collapsed = Raw([Line("任务开始：" + new string('甲', 50) + "任务完成：" + new string('乙', 50), "")]);
        Program.Assert(new FrameRecognitionPipeline(new Arguments()).Commit(collapsed).Accepted,
            "构造的塌缩帧须证明独立线程历史会影响结果");
        Program.Assert(!pipeline.Commit(collapsed).Accepted,
            "全局回放必须保留跨片段历史，以正确拒绝塌缩帧");
        var next = Raw([Line("任务完成：另一个任务", "12:00:02")]);
        Program.Assert(pipeline.Commit(next).NewLines.Single().Content == "任务完成：另一个任务", "后续真实新日志仍须输出");
        Console.WriteLine("PASS: global cross-segment deduplication and history-dependent rejection");
    }

    private static OcrEngine.OcrLineInfo Line(string content, string time)
        => new() { RawText = $"{time} {content}".Trim(), Content = content, TimeText = time };
    private static FrameRecognition Raw(List<OcrEngine.OcrLineInfo> lines)
    {
        var candidate = new OcrCandidateResult(new("fixture", 8, OcrEngine.OcrPreprocessMode.DetailPreserving),
            new() { Lines = lines }, new(0, "fixture", []), 1);
        return new(true, new(candidate, [candidate], 1), null, 1);
    }
}
