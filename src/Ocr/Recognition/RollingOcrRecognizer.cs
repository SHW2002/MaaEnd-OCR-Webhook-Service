using System.Diagnostics;
using System.Drawing;

namespace MaaEnd_Log_Retransmitter.Ocr;

internal static class RollingOcrRecognizer
{
    public static int CandidateCount => 1;

    public static async Task<OcrRecognitionBatch> RecognizeRollingAsync(Bitmap bitmap,
        string languageTag, bool saveDebugImages)
    {
        // Preserve master's rolling OCR: one 8x detail-preserving candidate.
        var stopwatch = Stopwatch.StartNew();
        var spec = new OcrCandidateSpec("detail", 8, OcrEngine.OcrPreprocessMode.DetailPreserving);
        var result = await OcrEngine.RecognizeWithWordsAsync(bitmap, languageTag,
            spec.Upscale, spec.PreprocessMode, saveDebugImages);
        var candidate = new OcrCandidateResult(spec, result, null, stopwatch.ElapsedMilliseconds);
        return new(candidate, [candidate], stopwatch.Elapsed.TotalMilliseconds);
    }
}
