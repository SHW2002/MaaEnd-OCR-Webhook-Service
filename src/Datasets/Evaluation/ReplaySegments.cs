namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed record ReplaySegment(int Index, int StartPosition, int EndPosition, double StartMs, double EndMs);

internal static class ReplaySegments
{
    public const int MaximumWorkers = 8;
    public static int DefaultWorkers => Math.Clamp(Environment.ProcessorCount / 2, 1, 4);

    public static IReadOnlyList<ReplaySegment> Split(IReadOnlyList<DatasetFrame> frames, int workers)
    {
        if (workers is < 1 or > MaximumWorkers) throw new ArgumentOutOfRangeException(nameof(workers));
        if (frames.Count == 0) return [];
        var start = frames[0].OffsetMs;
        var duration = frames[^1].OffsetMs - start;
        if (duration == 0) return [new(0, 0, frames.Count, start, start)];
        var segments = new List<ReplaySegment>();
        var position = 0;
        for (var i = 0; i < workers; i++)
        {
            var end = start + duration * (i + 1) / workers;
            var next = position;
            if (i == workers - 1) next = frames.Count;
            else while (next < frames.Count && frames[next].OffsetMs < end) next++;
            if (next > position)
                segments.Add(new(segments.Count, position, next, start + duration * i / workers, end));
            position = next;
        }
        return segments;
    }
}
