using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class MetricTests
{
    public static void Run()
    {
        Program.Assert(TextMetrics.Distance("任务完成", "任务完咸") == 1, "替换字符距离错误");
        Program.Assert(TextMetrics.Distance("abc", "axbc") == 1, "插入字符距离错误");
        Program.Assert(TextMetrics.Distance("a🦊", "a🐱") == 1 && TextMetrics.CharacterCount("a🦊") == 2,
            "CER 应按 Unicode 字符统计，不能将代理对算两次");
        var truth = new DatasetAnnotations
        {
            Frames = new() { [0] = new() { Reviewed = true, Lines = [new("12:00:00", "任务开始"), new("12:00:01", "任务完成")] } }
        };
        var frame = new EvaluatedFrame(0, 0, true, true, "detail", 10, null,
            [new("12:00:01", "任务完成")], [], 0, 0, 1, 1);
        var metrics = AccuracyCalculator.Calculate([frame], truth, false);
        Program.Assert(metrics.ExactLineRate == 0.5 && metrics.TimeAccuracy == 0.5, "漏行必须计错且不能错配后续时间");
        Program.Assert(metrics.CharacterErrorRate > 0, "漏行必须提高 CER");
        var incorrectTime = frame with { Lines = [new("00:00:00", "任务开始"), new("00:00:01", "任务完成")] };
        var bodyMetrics = AccuracyCalculator.Calculate([incorrectTime], truth, false);
        Program.Assert(bodyMetrics.CharacterErrorRate == 0 && bodyMetrics.ContentLineRate == 1,
            "正确正文不应因时间识别错误降低正文准确度");
        Program.Assert(bodyMetrics.ExactLineRate == 0 && bodyMetrics.TimeAccuracy == 0, "时间错误仍应在辅助指标中保留");
        truth.Frames[0].Reviewed = false;
        Program.Assert(AccuracyCalculator.Calculate([frame], truth, false).CharacterErrorRate == null, "未标注不能生成准确率");
        Program.Assert(TextMetrics.Normalize("任务 开始：Ａ", true) == "任务开始:A", "排版归一错误");
        var events = AccuracyCalculator.CalculateEvents(
            [new("12:00:00", "同内容", 0), new("12:00:01", "同内容", 100)],
            [new("12:00:00", "同内容", 50), new("12:00:00", "同内容", 60), new("12:00:01", "同内容", 120)]);
        Program.Assert(events.Correct == 2 && events.Precision == 2.0 / 3 && events.Recall == 1, "事件需一对一匹配，重复输出计错");
        Program.Assert(events.F1 == 0.8, "事件 F1 计算错误");
        var empty = AccuracyCalculator.CalculateEvents([], []);
        Program.Assert(empty.F1 == null, "无样本不能显示 100% F1");
        Console.WriteLine("PASS: metrics, missing lines, normalization, duplicate events, unreviewed/empty truth");
    }
}
