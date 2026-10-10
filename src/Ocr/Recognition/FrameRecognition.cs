using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Ocr;

internal sealed record FrameRecognition(
    bool AnchorFound,
    OcrRecognitionBatch? Recognition,
    RollingOcrOutputFilter.Result? Filter,
    double TotalMilliseconds,
    string? Error = null,
    System.Drawing.Rectangle? Crop = null)
{
    public bool Accepted => AnchorFound && Filter?.Accepted == true;
    public IReadOnlyList<OcrEngine.OcrLineInfo> Lines => Recognition?.Selected.Result.Lines ?? [];
    public IReadOnlyList<RollingOcrOutputFilter.OutputLine> NewLines => Filter?.NewLines ?? [];
}
