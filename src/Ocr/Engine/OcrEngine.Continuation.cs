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
    private static List<OcrLineInfo> MergeContinuationLines(List<OcrLineInfo> lines)
    {
        var merged = new List<OcrLineInfo>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.TimeText) && merged.Count > 0 && ShouldMergeContinuationLine(merged[^1], line))
            {
                var previous = merged[^1];
                previous.RawText = NormalizeOcrSpacing(CombineContinuationText(previous.RawText, line.RawText));
                previous.Content = NormalizeOcrSpacing(CombineContinuationText(previous.Content, line.Content));
                previous.Words.AddRange(line.Words);
                continue;
            }

            merged.Add(line);
        }

        return merged;
    }

    private static bool ShouldMergeContinuationLine(OcrLineInfo previous, OcrLineInfo current)
    {
        if (string.IsNullOrWhiteSpace(previous.TimeText)) return false;
        if (current.Words.Count == 0) return false;
        if (string.IsNullOrWhiteSpace(current.Content)) return false;

        var currentText = current.Content.Trim();
        if (currentText.Length > 32) return false;

        int previousFirstX = GetFirstWordX(previous.Words);
        int currentFirstX = GetFirstWordX(current.Words);
        if (previousFirstX >= 0 && currentFirstX >= 0 && currentFirstX <= previousFirstX + 8)
        {
            return false;
        }

        int previousBottom = GetMaxBottom(previous.Words);
        int currentTop = GetMinTop(current.Words);
        double averageHeight = GetAverageHeight(previous.Words.Concat(current.Words));
        if (previousBottom >= 0 && currentTop >= 0 && averageHeight > 0)
        {
            int verticalGap = currentTop - previousBottom;
            if (verticalGap > averageHeight * 1.2)
            {
                return false;
            }
        }

        return true;
    }

    private static string CombineContinuationText(string left, string right)
    {
        left = left.TrimEnd();
        right = right.TrimStart();

        if (string.IsNullOrEmpty(left)) return right;
        if (string.IsNullOrEmpty(right)) return left;

        char leftLast = left[^1];
        char rightFirst = right[0];
        if (ShouldOmitSeparator(leftLast, rightFirst))
        {
            return left + right;
        }

        return left + " " + right;
    }

    private static bool ShouldOmitSeparator(char leftLast, char rightFirst)
    {
        return IsOpeningPunctuation(leftLast)
            || IsClosingPunctuation(rightFirst)
            || leftLast == ':'
            || leftLast == '：'
            || rightFirst == ','
            || rightFirst == '，';
    }

    private static bool IsOpeningPunctuation(char c)
    {
        return c == '('
            || c == '（'
            || c == '['
            || c == '［'
            || c == '{'
            || c == '｛';
    }

    private static bool IsClosingPunctuation(char c)
    {
        return c == ')'
            || c == '）'
            || c == ']'
            || c == '］'
            || c == '}'
            || c == '｝';
    }

}
