using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Infrastructure;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace MaaEnd_Log_Retransmitter.Ocr;

internal static partial class OcrEngine
{
    private static string? TryNormalizeTimeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var normalized = NormalizeTimeCandidate(text);
        if (normalized == null) return null;

        var digits = new string(normalized.Where(char.IsDigit).ToArray());
        if (digits.Length == 4 && IsValidTimeComponents(digits[..2], digits[2..4]))
            return $"{digits[..2]}:{digits[2..4]}";

        if (digits.Length == 6 && IsValidTimeComponents(digits[..2], digits[2..4], digits[4..6]))
            return $"{digits[..2]}:{digits[2..4]}:{digits[4..6]}";

        if (digits.Length is >= 7 and <= 9)
        {
            if (!IsValidTimeComponents(digits[..2], digits[2..4], digits[4..6])) return null;
            var fraction = digits[6..];
            return $"{digits[..2]}:{digits[2..4]}:{digits[4..6]}.{fraction}";
        }

        return null;
    }

    private static bool IsValidTimeComponents(string hourText, string minuteText, string? secondText = null)
    {
        if (!int.TryParse(hourText, out var hour) || hour is < 0 or > 23) return false;
        if (!int.TryParse(minuteText, out var minute) || minute is < 0 or > 59) return false;
        if (secondText == null) return true;
        return int.TryParse(secondText, out var second) && second is >= 0 and <= 59;
    }


    private static string? NormalizeTimeCandidate(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c) || c == ' ' || c == '　')
                continue;

            if (char.IsDigit(c))
            {
                sb.Append(c);
                continue;
            }

            if (c == '：' || c == '﹕' || c == '︰' || c == ':' || c == '·' || c == '•' || c == '．' || c == '.')
            {
                sb.Append(':');
                continue;
            }

            if (c == ',' || c == '，')
            {
                sb.Append('.');
                continue;
            }

            return null;
        }

        while (sb.Length > 0 && (sb[^1] == ':' || sb[^1] == '.'))
            sb.Length--;

        return sb.Length == 0 ? null : sb.ToString();
    }

}
