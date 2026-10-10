namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed record AnnotationGroup(int StartPosition, int EndPosition, double OffsetMs, int LineCount, string Preview)
{
    public override string ToString()
        => $"第 {StartPosition + 1}–{EndPosition + 1} 帧 · {OffsetMs / 1000:F2}s · {LineCount} 条 · {Preview}";
}

internal static class AnnotationTimeline
{
    public static List<AnnotationGroup> Build(DatasetManifest manifest, DatasetAnnotations annotations)
    {
        var groups = new List<AnnotationGroup>();
        List<TruthLine>? previous = null;
        for (var position = 0; position < manifest.Frames.Count; position++)
        {
            var frame = manifest.Frames[position];
            var lines = annotations.Frames.GetValueOrDefault(frame.Index)?.Lines ?? [];
            if (previous != null && previous.SequenceEqual(lines)) continue;
            if (groups.Count > 0) groups[^1] = groups[^1] with { EndPosition = position - 1 };
            var preview = lines.Count == 0 ? "空日志" : lines[^1].Content.ReplaceLineEndings(" ");
            if (preview.Length > 100) preview = preview[..100] + "…";
            groups.Add(new(position, position, frame.OffsetMs, lines.Count, preview));
            previous = lines;
        }
        if (groups.Count > 0) groups[^1] = groups[^1] with { EndPosition = manifest.Frames.Count - 1 };
        return groups;
    }
}
