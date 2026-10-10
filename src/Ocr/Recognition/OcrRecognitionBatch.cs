namespace MaaEnd_Log_Retransmitter.Ocr;

internal sealed record OcrRecognitionBatch(
    OcrCandidateResult Selected,
    IReadOnlyList<OcrCandidateResult> Candidates,
    double TotalMilliseconds);
