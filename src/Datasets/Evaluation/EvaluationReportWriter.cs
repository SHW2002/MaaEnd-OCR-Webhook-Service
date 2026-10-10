using System.Text;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class EvaluationReportWriter
{
    public static string Format(EvaluationReport report)
    {
        var run = report.Result;
        var m = run.Strict;
        var text = new StringBuilder("# OCR 数据集评估\n\n");
        text.AppendLine($"会话：{report.SessionId}  模式：{report.Mode}");
        text.AppendLine($"算法：当前分支正式方案；线程设置：{report.RequestedWorkers}；实际片段：{report.WorkerCount}");
        text.AppendLine($"评估总耗时（不含报告写入）：{report.WallMilliseconds / 1000:F1} 秒");
        text.AppendLine($"总帧数：{report.TotalFrames}；人工确认帧：{report.ReviewedFrames}\n");
        text.AppendLine(report.Mode == "production-timing"
            ? $"各段按正式频率采样：上轮识别耗时 + {report.Recognition.RollingIntervalMs}ms 等待后取下一帧；锚点失败时使用重试间隔。\n"
            : "逐帧模式：处理全部录制帧。\n");
        text.AppendLine("| 处理帧 | 已标注处理帧 | 正文 CER | 正文整行正确率 | 时间与正文全对 | 时间准确率 | 锚点成功率 | 中位耗时 ms | P95 ms |");
        text.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        text.AppendLine($"| {run.Frames.Count} | {m.EvaluatedFrames} | {Percent(m.CharacterErrorRate)} | {Percent(m.ContentLineRate)} | {Percent(m.ExactLineRate)} | {Percent(m.TimeAccuracy)} | {Percent(m.AnchorSuccessRate)} | {run.MedianMilliseconds:F1} | {run.P95Milliseconds:F1} |");
        text.AppendLine("\n## 统一排版后的文字指标\n");
        text.AppendLine($"正文 CER {Percent(run.Normalized.CharacterErrorRate)}，正文整行正确率 {Percent(run.Normalized.ContentLineRate)}");
        text.AppendLine("\n## 分场景严格指标\n");
        foreach (var scenario in run.Scenarios)
            text.AppendLine($"- {scenario.Key}：{scenario.Value.EvaluatedFrames} 帧，正文 CER {Percent(scenario.Value.CharacterErrorRate)}，正文整行正确率 {Percent(scenario.Value.ContentLineRate)}");
        text.AppendLine("\n## 时间片段\n");
        text.AppendLine("| 片段 | 起止秒数 | 原始帧 | 识别样本 | 工作线程耗时秒 | 状态 |");
        text.AppendLine("| ---: | --- | ---: | ---: | ---: | --- |");
        foreach (var segment in run.Segments)
            text.AppendLine($"| {segment.Index} | {segment.StartMs / 1000:F1}–{segment.EndMs / 1000:F1} | {segment.SourceFrames} | {segment.ProcessedFrames} | {segment.WallMilliseconds / 1000:F1} | {segment.StopReason ?? "完成"} |");
        text.AppendLine("\n## 最终事件\n");
        var events = run.EventMetrics;
        text.AppendLine(events == null ? "事件真值未人工确认，未计算 P/R/F1。"
            : $"输出 {events.Output}，真值 {events.Expected}，正确 {events.Correct}，P {Percent(events.Precision)}，R {Percent(events.Recall)}，F1 {Percent(events.F1)}，中位回放延迟 {events.MedianLatencyMs?.ToString("F1") ?? "—"}ms");
        text.AppendLine("\n## 指标口径\n");
        text.AppendLine("- 仅评估当前分支正式方案，共用锚点定位、裁剪、OCR、候选选择、解析、过滤与去重代码；单候选方案没有规则评分，评分字段为 null。");
        text.AppendLine("- 未人工确认的帧不参与准确度计算；已确认空帧可以统计误识别字符。分母为零显示 —，不表示 100%。");
        text.AppendLine("- 正文 CER 与正文整行正确率均忽略时间；CER 为正文按顺序连接（含换行）的编辑距离 / 真值字符数，可超过 100%。正文整行正确率要求对齐后的正文完全正确，漏行算错。");
        text.AppendLine("- 时间与正文全对、时间准确率作为辅助指标；锚点成功率包含已缓存定位。统一排版仅做 Unicode FormKC 和空白归一，不纠正错字。");
        text.AppendLine("- 按录制时间平均划分连续片段，各线程在自己的片段内按顺序采样识别；只读取和校验实际样本，不额外等待真实时间。");
        text.AppendLine("- 每段首帧重新定位窗口，片段起点独立采样，空片段不启动线程；分段边界及并行资源争用可能改变采样位置。设置 1 个线程可连续回放整个会话。");
        text.AppendLine("- 所有片段结果最终按时间顺序统一过滤与去重，跨段重复日志不会因线程历史独立而重复输出。锚点重试上限在各段单独生效。");
        text.AppendLine("- 单帧耗时包含定位（需要时）、裁剪、所有候选、评分、解析及过滤，不含磁盘读取、队列等待、录制与网络；过滤在统一回放时执行，不参与段内下一帧调度。");
        text.AppendLine("- 并行单帧耗时受 CPU 争用影响，不能直接代表正式单独运行的性能；性能比较请使用 1 个线程。");
        text.AppendLine("- 事件按严格时间与正文一对一匹配，重复输出算错报；回放延迟为帧时间 + 单帧处理耗时 − 真值首次出现时间，不代表并行评估墙钟延迟，也不含网络。评估不发送 Webhook。");
        return text.ToString();
    }
    private static string Percent(double? value) => value.HasValue ? $"{value:P2}" : "—";
}
