using System.Text;
using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.App;

internal sealed partial class RollingOcrOutputFilter
{
    private static bool IsUsableContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var category = ClassifyContent(content);
        return category is LineCategory.TaskStarted or LineCategory.TaskCompleted or LineCategory.TaskFailed
            ? IsUsableStructuredContent(content)
            : IsUsableGeneralContent(content);
    }

    private static bool IsUsableStructuredContent(string content)
    {
        if (content.Length < 2 || content.Length > 48) return false;
        if (CountStructuredMarkers(content) > 1) return false;
        if (Regex.Matches(content, @"\d{1,2}[:：]\d{2}").Count > 0) return false;

        int signalChars = content.Count(c => IsReadableSignalChar(c));
        if (signalChars < 2) return false;

        var payload = content[(content.IndexOf('：') + 1)..].Trim();
        payload = TrimNoisePrefix(payload, aggressive: true);
        return payload.Length >= 2;
    }

    private static bool IsUsableGeneralContent(string content)
    {
        if (content.Length < 2 || content.Length > 72) return false;

        int signalChars = content.Count(c => IsReadableSignalChar(c));
        if (signalChars < 2) return false;

        bool hasChinese = content.Any(c => c >= 0x4E00 && c <= 0x9FFF);
        bool hasLetterOrDigit = content.Any(char.IsLetterOrDigit);
        if (!hasChinese && !hasLetterOrDigit) return false;

        if (CountStructuredMarkers(content) >= 3) return false;
        return true;
    }

    private static int CountStructuredMarkers(string content)
    {
        return StructuredCollapseMarkers.Sum(marker => CountOccurrences(content, marker));
    }

    private static bool IsReadableSignalChar(char c)
    {
        return c >= 0x4E00 && c <= 0x9FFF
            || char.IsLetterOrDigit(c);
    }

    private static LineCategory ClassifyContent(string content)
    {
        if (content.StartsWith("任务开始：", StringComparison.Ordinal)) return LineCategory.TaskStarted;
        if (content.StartsWith("任务完成：", StringComparison.Ordinal)) return LineCategory.TaskCompleted;
        if (content.StartsWith("任务失败：", StringComparison.Ordinal)) return LineCategory.TaskFailed;
        if (content.StartsWith("开始", StringComparison.Ordinal)
            || content.StartsWith("正在", StringComparison.Ordinal)
            || content.StartsWith("没有", StringComparison.Ordinal)
            || content.StartsWith("领取", StringComparison.Ordinal)
            || content.StartsWith("进入", StringComparison.Ordinal)
            || content.StartsWith("画面", StringComparison.Ordinal)
            || content.StartsWith("好友", StringComparison.Ordinal))
        {
            return LineCategory.Activity;
        }

        return LineCategory.Other;
    }

    private static string BuildCanonicalContent(string content, LineCategory category)
    {
        var normalized = content;
        if (category is LineCategory.TaskStarted or LineCategory.TaskCompleted or LineCategory.TaskFailed)
        {
            var match = StructuredEventPattern.Match(content);
            if (match.Success)
            {
                var prefix = match.Groups["prefix"].Value;
                var payload = TrimNoisePrefix(match.Groups["payload"].Value, aggressive: true);
                normalized = prefix + payload;
            }
        }

        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (c is '：' or ':' or '．' or '.' or ',' or '，' or '、' or '·' or '•' or '﹕' or '︰' or '-') continue;
            sb.Append(NormalizeDigitChar(c));
        }

        return sb.ToString();
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        normalized = normalized.Replace('﹕', '：').Replace('︰', '：');
        normalized = Regex.Replace(normalized, @"\s+([，。；：！？、])", "$1");
        normalized = Regex.Replace(normalized, @"([（\(\[［\{｛])\s+", "$1");
        normalized = Regex.Replace(normalized, @"\s+([）\)\]］\}｝])", "$1");
        return normalized;
    }


    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value)) return 0;

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

}
