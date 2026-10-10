using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class ReplaySamplerTests
{
    public static void Run()
    {
        var offsets = new double[] { 30, 100, 3000, 3500, 3530, 3530, 3600, 6000, 7200 };
        var frames = offsets.Select((offset, index) => new DatasetFrame(index * 3, "fixture.png", offset,
            DateTimeOffset.UtcNow, 80, 60, 96, "fixture", 0, "")).ToArray();
        Program.Assert(ReplaySampler.NextPosition(frames, 0, 500, 3000, true) == 4,
            "采样须按输入时间 + 识别耗时 + 正式间隔定位，不能只按固定帧数跳跃");
        Program.Assert(ReplaySampler.NextPosition(frames, 0, 0, 3470, true) == 3,
            "恰好等于下一次采集时刻的帧不能遗漏");
        Program.Assert(ReplaySampler.NextPosition(frames, 4, 800, 3000, true) == frames.Length,
            "录制结束后不能补录不足等待间隔的尾帧");
        Program.Assert(ReplaySampler.NextPosition(frames, 0, 500, 3000, false) == 1,
            "显式逐帧模式须保留全部输入");
        var config = new Arguments { RollingIntervalMs = 3000, RetryInterval = 1000 };
        var rejected = new FrameRecognition(true, null, null, 500, "结果被过滤");
        var unlocalized = rejected with { AnchorFound = false };
        Program.Assert(FrameRecognitionPipeline.NextDelay(rejected, config) == 3000,
            "已定位但被拒绝的识别也须遵守正式等待时间");
        Program.Assert(FrameRecognitionPipeline.NextDelay(unlocalized, config) == 1000,
            "定位失败须使用正式重试间隔");
        Console.WriteLine("PASS: sampling timestamps, processing time, irregular capture gaps, boundaries, production delay rules");
    }
}
