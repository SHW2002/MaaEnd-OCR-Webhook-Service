namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed class DatasetAnnotations
{
    public Dictionary<int, FrameAnnotation> Frames { get; set; } = [];
    public bool EventsReviewed { get; set; }
    public List<ExpectedEvent> Events { get; set; } = [];
}

internal sealed class FrameAnnotation
{
    public bool Reviewed { get; set; }
    public bool AnchorVisible { get; set; } = true;
    public string Scenario { get; set; } = "";
    // Only logs below the anchor, one entry per visible logical log line.
    public List<TruthLine> Lines { get; set; } = [];
}

internal sealed record TruthLine(string TimeText, string Content);
internal sealed record ExpectedEvent(string TimeText, string Content, double FirstVisibleMs);
