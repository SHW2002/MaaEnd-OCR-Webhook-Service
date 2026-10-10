using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal sealed class DatasetManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedUtc { get; set; }
    public string Format { get; set; } = "lossless-png-sequence";
    public string Scenario { get; set; } = "";
    public string OperatingSystem { get; set; } = Environment.OSVersion.ToString();
    public string Runtime { get; set; } = Environment.Version.ToString();
    public string ApplicationVersion { get; set; } = Infrastructure.BuildInfo.Version;
    public string BuildId { get; set; } = Infrastructure.BuildInfo.Id;
    public string[] AvailableOcrLanguages { get; set; } = [];
    public Arguments Recognition { get; set; } = new();
    public int CaptureIntervalMs { get; set; }
    public int MissedCaptureSlots { get; set; }
    public List<DatasetFrame> Frames { get; set; } = [];
    public List<DatasetCaptureError> CaptureErrors { get; set; } = [];
}

internal sealed record DatasetFrame(int Index, string File, double OffsetMs, DateTimeOffset CapturedUtc,
    int Width, int Height, uint Dpi, string WindowTitle, double CaptureMilliseconds, string Sha256,
    string CaptureMethod = "");

internal sealed record DatasetCaptureError(double OffsetMs, string Message);
