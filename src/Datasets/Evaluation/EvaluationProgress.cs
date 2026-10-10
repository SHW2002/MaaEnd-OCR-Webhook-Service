namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed class EvaluationProgress(IReadOnlyList<ReplaySegment> segments, IProgress<string>? output)
{
    private readonly object _gate = new();
    private readonly int[] _samples = new int[segments.Count];
    private readonly double[] _elapsed = new double[segments.Count];
    private readonly bool[] _finished = new bool[segments.Count];

    public void Update(int segment, int samples, double offset, bool finished = false)
    {
        lock (_gate)
        {
            var range = segments[segment];
            _samples[segment] = samples;
            _elapsed[segment] = Math.Clamp(offset - range.StartMs, 0, range.EndMs - range.StartMs);
            _finished[segment] = finished;
            var duration = segments.Sum(x => x.EndMs - x.StartMs);
            var percent = duration > 0 ? _elapsed.Sum() / duration : _finished.All(x => x) ? 1 : 0;
            output?.Report($"{segments.Count} 线程 · 已识别 {_samples.Sum()} 个样本 · 总进度 {percent:P1} · "
                + $"已结束 {_finished.Count(x => x)}/{segments.Count} 段");
        }
    }
}
