namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class ReplaySampler
{
    public static int NextPosition(IReadOnlyList<DatasetFrame> frames, int currentPosition,
        double processingMilliseconds, int delayMilliseconds, bool sampleFrames)
    {
        if (!sampleFrames) return currentPosition + 1;
        var due = frames[currentPosition].OffsetMs + processingMilliseconds + delayMilliseconds;
        var left = currentPosition + 1;
        var right = frames.Count;
        while (left < right)
        {
            var middle = left + (right - left) / 2;
            if (frames[middle].OffsetMs < due) left = middle + 1;
            else right = middle;
        }
        return left;
    }
}
