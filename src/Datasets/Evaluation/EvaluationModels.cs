using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed class EvaluationReport
{
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string SessionId { get; set; } = "";
    public string Mode { get; set; } = "";
    public string ExecutionMode { get; set; } = "parallel-segments";
    public int RequestedWorkers { get; set; }
    public int WorkerCount { get; set; }
    public double WallMilliseconds { get; set; }
    public string OperatingSystem { get; set; } = Environment.OSVersion.ToString();
    public string Runtime { get; set; } = Environment.Version.ToString();
    public string ApplicationVersion { get; set; } = Infrastructure.BuildInfo.Version;
    public string BuildId { get; set; } = Infrastructure.BuildInfo.Id;
    public string[] AvailableOcrLanguages { get; set; } = [];
    public Arguments Recognition { get; set; } = new();
    public int TotalFrames { get; set; }
    public int ReviewedFrames { get; set; }
    public EvaluationResult Result { get; set; } = new();
}

internal sealed class EvaluationResult
{
    public List<EvaluatedFrame> Frames { get; set; } = [];
    public List<ObservedEvent> Events { get; set; } = [];
    public AccuracyMetrics Strict { get; set; } = new();
    public AccuracyMetrics Normalized { get; set; } = new();
    public EventMetrics? EventMetrics { get; set; }
    public Dictionary<string, AccuracyMetrics> Scenarios { get; set; } = [];
    public double MedianMilliseconds { get; set; }
    public double P95Milliseconds { get; set; }
    public List<SegmentEvaluation> Segments { get; set; } = [];
}

internal sealed record SegmentEvaluation(int Index, double StartMs, double EndMs, int SourceFrames,
    int ProcessedFrames, double WallMilliseconds, string? StopReason);

internal sealed record EvaluatedFrame(int Index, double OffsetMs, bool AnchorFound, bool Accepted,
    string? SelectedCandidate, double TotalMilliseconds, string? Error, IReadOnlyList<TruthLine> Lines,
    IReadOnlyList<CandidateEvaluation> Candidates, int? CropX, int? CropY, int? CropWidth, int? CropHeight);
internal sealed record CandidateEvaluation(string Name, int? Score, long Milliseconds, string Text);
internal sealed record ObservedEvent(string TimeText, string Content, double DetectedMs, double SourceOffsetMs = double.MaxValue);

internal sealed class AccuracyMetrics
{
    public int EvaluatedFrames { get; set; }
    public long GroundTruthCharacters { get; set; }
    public long CharacterErrors { get; set; }
    public int GroundTruthLines { get; set; }
    public int CorrectContentLines { get; set; }
    public int ExactLines { get; set; }
    public int CorrectTimes { get; set; }
    public int GroundTruthAnchors { get; set; }
    public int FoundAnchors { get; set; }
    public double? CharacterErrorRate => GroundTruthCharacters == 0 ? null : (double)CharacterErrors / GroundTruthCharacters;
    public double? ExactLineRate => GroundTruthLines == 0 ? null : (double)ExactLines / GroundTruthLines;
    public double? ContentLineRate => GroundTruthLines == 0 ? null : (double)CorrectContentLines / GroundTruthLines;
    public double? TimeAccuracy => GroundTruthLines == 0 ? null : (double)CorrectTimes / GroundTruthLines;
    public double? AnchorSuccessRate => GroundTruthAnchors == 0 ? null : (double)FoundAnchors / GroundTruthAnchors;
}

internal sealed record EventMetrics(int Expected, int Output, int Correct, double? Precision,
    double? Recall, double? F1, double? MedianLatencyMs);
