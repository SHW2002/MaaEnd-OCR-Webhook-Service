using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class ReplaySegmentTests
{
    public static void Run()
    {
        var frames = Frames([0, 1, 2, 2500, 2500, 5000, 7500, 10000]);
        var segments = ReplaySegments.Split(frames, 4);
        Program.Assert(segments.Count == 4 && segments.All(x => x.EndMs - x.StartMs == 2500),
            "必须按时间均分而非按帧数均分");
        Program.Assert(segments[1].StartPosition == 3 && segments[1].EndPosition == 5,
            "边界及相同时间戳的帧必须归属同一段，不能重复或遗漏");
        Program.Assert(segments.SelectMany(x => Enumerable.Range(x.StartPosition, x.EndPosition - x.StartPosition))
            .SequenceEqual(Enumerable.Range(0, frames.Count)), "所有源帧必须恰好属于一个时间片段");
        Program.Assert(segments[3].EndPosition == frames.Count, "必须包含录制最后一帧");
        var sparse = ReplaySegments.Split(Frames([30, 35, 10030]), 8);
        Program.Assert(sparse.Count == 2 && sparse.All(x => x.EndPosition > x.StartPosition),
            "空时间段不能创建没有输入的工作线程");
        Program.Assert(ReplaySegments.Split(Frames([10, 10, 10]), 4).Single().EndPosition == 3,
            "零时长输入也必须保留全部帧");
        Program.Assert(ReplaySegments.Split(frames, 1).Single() is { StartPosition: 0, EndPosition: 8 },
            "一个线程必须连续处理完整会话");
        Console.WriteLine("PASS: equal time segments, irregular frame rates, exact boundaries, duplicate timestamps, empty segments, single worker");
    }

    private static IReadOnlyList<DatasetFrame> Frames(double[] offsets)
        => offsets.Select((offset, index) => new DatasetFrame(index * 3, "fixture.png", offset,
            DateTimeOffset.UtcNow, 80, 60, 96, "fixture", 0, "")).ToArray();
}
