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
    private static (string TimeText, string Content)? TrySplitTimeAndContent(List<OcrWordInfo> orderedWords)
    {
        if (orderedWords.Count < 2) return null;

        int? bestSplitIndex = null;
        double bestGapScore = 0;
        string? bestTimeText = null;

        for (int i = 0; i < orderedWords.Count - 1; i++)
        {
            var previous = orderedWords[i];
            var current = orderedWords[i + 1];
            int gap = current.X - (previous.X + previous.Width);
            if (gap <= 0) continue;

            var leftText = BuildLineText(orderedWords.Take(i + 1).ToList());
            var normalizedTimeText = TryNormalizeTimeText(leftText);
            if (normalizedTimeText == null) continue;

            double previousCharWidth = previous.Text.Length > 0 ? (double)previous.Width / previous.Text.Length : previous.Width;
            double currentCharWidth = current.Text.Length > 0 ? (double)current.Width / current.Text.Length : current.Width;
            double typicalCharWidth = Math.Max(4.0, Math.Min(previousCharWidth, currentCharWidth));
            double gapScore = gap / typicalCharWidth;

            if (gapScore > bestGapScore)
            {
                bestGapScore = gapScore;
                bestSplitIndex = i;
                bestTimeText = normalizedTimeText;
            }
        }

        if (bestSplitIndex == null || bestGapScore < 1.8 || string.IsNullOrWhiteSpace(bestTimeText)) return null;

        string content = BuildLineText(orderedWords.Skip(bestSplitIndex.Value + 1).ToList());
        if (string.IsNullOrWhiteSpace(content)) return null;

        return (bestTimeText, content);
    }

    private static string BuildLineText(List<OcrWordInfo> lineWords)
    {
        var ordered = lineWords.OrderBy(word => word.X).ToList();
        var sb = new StringBuilder();

        for (int i = 0; i < ordered.Count; i++)
        {
            var word = ordered[i];
            if (i > 0)
            {
                var previous = ordered[i - 1];
                if (ShouldInsertSpace(previous, word)) sb.Append(' ');
            }

            sb.Append(word.Text);
        }

        return sb.ToString().Trim();
    }

    private static bool ShouldInsertSpace(OcrWordInfo previous, OcrWordInfo current)
    {
        int gap = current.X - (previous.X + previous.Width);
        if (gap <= 0) return false;

        double previousCharWidth = previous.Text.Length > 0 ? (double)previous.Width / previous.Text.Length : previous.Width;
        double currentCharWidth = current.Text.Length > 0 ? (double)current.Width / current.Text.Length : current.Width;
        double typicalCharWidth = Math.Max(4.0, Math.Min(previousCharWidth, currentCharWidth));
        return gap >= typicalCharWidth * 1.2;
    }

    private static string NormalizeOcrSpacing(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        normalized = Regex.Replace(normalized, @"(?<=[\p{IsCJKUnifiedIdeographs}\p{IsCJKSymbolsandPunctuation}\p{IsHalfwidthandFullwidthForms}])\s+(?=[\p{IsCJKUnifiedIdeographs}\p{IsCJKSymbolsandPunctuation}\p{IsHalfwidthandFullwidthForms}])", "");
        normalized = Regex.Replace(normalized, @"([（\(\[［\{｛])\s+", "$1");
        normalized = Regex.Replace(normalized, @"\s+([）\)\]］\}｝])", "$1");
        normalized = Regex.Replace(normalized, @"\s+([，。；：！？、])", "$1");
        normalized = Regex.Replace(normalized, @"([，。；：！？、])\s+", "$1");
        return normalized;
    }

    private static string SanitizeContentPrefix(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        var trimmed = content.TrimStart();
        if (trimmed.Length < 2) return content;

        char first = trimmed[0];
        char second = trimmed[1];
        if (IsSuspiciousIconPrefix(first) && IsCjkOrPunct(second))
        {
            return trimmed[1..].TrimStart();
        }

        return content;
    }

    private static string NormalizeStructuredContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        var normalized = content;

        // 修复固定前缀后面的图标/表情被 OCR 成 0/O/· 等噪声字符。
        normalized = Regex.Replace(normalized, @"^(任务开始|任务完成)\s+[0Oo○●·•．.]\s*(?=\S)", "$1：");

        // 如果固定前缀后直接跟正文但没有分隔符，则补一个中文冒号。
        normalized = Regex.Replace(normalized, @"^(任务开始|任务完成)(?![:：．.])\s+(?=\S)", "$1：");

        // 将被 OCR 成句点的分隔符统一回中文冒号。
        normalized = Regex.Replace(normalized, @"^(任务开始|任务完成)[．.]", "$1：");

        // 修复冒号后紧跟图标/表情被识别成 0/O/· 等噪声字符的情况。
        normalized = Regex.Replace(normalized, @"^(任务开始|任务完成)[:：][0Oo○●·•．.]\s*(?=\S)", "$1：");

        return NormalizeOcrSpacing(normalized);
    }

    private static bool IsSuspiciousIconPrefix(char c)
    {
        return c == '0'
            || c == 'O'
            || c == 'o'
            || c == '○'
            || c == '●'
            || c == '·'
            || c == '•'
            || c == '．'
            || c == '.';
    }

}
