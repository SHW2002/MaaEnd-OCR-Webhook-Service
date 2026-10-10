namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class AccuracyCalculator
{
    public static AccuracyMetrics Calculate(IReadOnlyList<EvaluatedFrame> frames, DatasetAnnotations annotations, bool normalized)
    {
        var metrics = new AccuracyMetrics();
        foreach (var frame in frames)
        {
            if (!annotations.Frames.TryGetValue(frame.Index, out var truth) || !truth.Reviewed) continue;
            metrics.EvaluatedFrames++;
            if (truth.AnchorVisible) { metrics.GroundTruthAnchors++; if (frame.AnchorFound) metrics.FoundAnchors++; }
            var expected = string.Join("\n", truth.Lines.Select(x => TextMetrics.Normalize(x.Content, normalized)));
            var actual = string.Join("\n", frame.Lines.Select(x => TextMetrics.Normalize(x.Content, normalized)));
            metrics.GroundTruthCharacters += TextMetrics.CharacterCount(expected);
            metrics.CharacterErrors += TextMetrics.Distance(expected, actual);
            metrics.GroundTruthLines += truth.Lines.Count;
            foreach (var pair in TextMetrics.Align(truth.Lines, frame.Lines, normalized))
            {
                if (pair.Actual == null) continue;
                var correctTime = pair.Expected.TimeText == pair.Actual.TimeText;
                var correctContent = TextMetrics.Normalize(pair.Expected.Content, normalized) == TextMetrics.Normalize(pair.Actual.Content, normalized);
                if (correctTime) metrics.CorrectTimes++;
                if (correctContent) metrics.CorrectContentLines++;
                if (correctTime && correctContent) metrics.ExactLines++;
            }
        }
        return metrics;
    }

    public static EventMetrics CalculateEvents(List<ExpectedEvent> expected, List<ObservedEvent> actual)
    {
        var remaining = expected.ToList();
        var latencies = new List<double>();
        foreach (var item in actual)
        {
            var index = remaining.FindIndex(x => x.TimeText == item.TimeText && x.Content == item.Content
                && x.FirstVisibleMs <= Math.Min(item.SourceOffsetMs, item.DetectedMs));
            if (index < 0) continue;
            latencies.Add(item.DetectedMs - remaining[index].FirstVisibleMs);
            remaining.RemoveAt(index);
        }
        int correct = latencies.Count;
        double? precision = actual.Count == 0 ? null : (double)correct / actual.Count;
        double? recall = expected.Count == 0 ? null : (double)correct / expected.Count;
        double? f1 = expected.Count + actual.Count == 0 ? null : 2.0 * correct / (expected.Count + actual.Count);
        return new(expected.Count, actual.Count, correct, precision, recall, f1,
            latencies.Count == 0 ? null : Percentile(latencies, 0.5));
    }

    public static double Percentile(IEnumerable<double> values, double fraction)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0) return 0;
        var position = (sorted.Length - 1) * fraction;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }
}
