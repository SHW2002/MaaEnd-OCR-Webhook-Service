namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed record AnnotationText(int FramePosition, DatasetFrame Frame, TruthLine Line);

internal static class AnnotationCatalog
{
    public static List<AnnotationText> Build(DatasetManifest manifest, DatasetAnnotations annotations)
    {
        var texts = new List<AnnotationText>();
        var seen = new HashSet<TruthLine>();
        for (var position = 0; position < manifest.Frames.Count; position++)
        {
            var frame = manifest.Frames[position];
            if (!annotations.Frames.TryGetValue(frame.Index, out var annotation)) continue;
            foreach (var line in annotation.Lines)
                if (seen.Add(line)) texts.Add(new(position, frame, line));
        }
        return texts;
    }
}
