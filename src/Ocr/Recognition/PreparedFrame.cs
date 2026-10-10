using System.Drawing;

namespace MaaEnd_Log_Retransmitter.Ocr;

internal sealed record PreparedFrame(Rectangle? Crop, double Milliseconds, string? Error = null);
