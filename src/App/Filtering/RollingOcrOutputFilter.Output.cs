using System.Text;
using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.App;

internal sealed partial class RollingOcrOutputFilter
{
    private static OutputLine? TryBuildOutputLine(OcrEngine.OcrLineInfo line)
    {
        var rawText = NormalizeText(line.RawText);
        var content = NormalizeContent(line.Content);
        var timeText = NormalizeTimeText(line.TimeText);

        if (string.IsNullOrWhiteSpace(content))
        {
            content = NormalizeContent(rawText);
        }

        if (string.IsNullOrWhiteSpace(timeText))
        {
            if (TryExtractLeadingTime(content, out var extractedTime, out var remainingContent)
                || TryExtractLeadingTime(rawText, out extractedTime, out remainingContent))
            {
                timeText = extractedTime;
                content = NormalizeContent(remainingContent);
            }
        }

        content = NormalizeContent(content);
        if (string.IsNullOrWhiteSpace(content)) return null;
        if (!IsUsableContent(content)) return null;

        var category = ClassifyContent(content);
        var canonicalContent = BuildCanonicalContent(content, category);
        if (string.IsNullOrWhiteSpace(canonicalContent)) return null;

        if (string.IsNullOrWhiteSpace(timeText))
        {
            timeText = ExtractLooseTime(rawText) ?? OcrEngine.OcrLineInfo.GetUnknownTimeText();
        }

        return new OutputLine(
            timeText,
            content,
            category,
            canonicalContent,
            $"{category}|{canonicalContent}",
            new OcrEngine.OcrLineInfo
            {
                RawText = line.RawText,
                TimeText = timeText,
                Content = content,
                Words = line.Words
            });
    }


    private static string NormalizeContent(string content)
    {
        var normalized = NormalizeText(content);
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;

        var structuredMatch = StructuredEventPattern.Match(normalized);
        if (structuredMatch.Success)
        {
            var prefix = structuredMatch.Groups["prefix"].Value;
            var payload = SanitizePayload(structuredMatch.Groups["payload"].Value);
            if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
            return $"{prefix}：{payload}";
        }

        return SanitizeGeneralContent(normalized);
    }

    private static string SanitizePayload(string payload)
    {
        var sanitized = NormalizeText(payload);
        sanitized = Regex.Replace(sanitized, @"^[：:．.，,、;；\-\s]+", string.Empty);
        sanitized = TrimNoisePrefix(sanitized, aggressive: true);
        sanitized = Regex.Replace(sanitized, @"\s+[：:．.，,、;；]+", " ");
        sanitized = sanitized.Trim();
        return sanitized;
    }

    private static string SanitizeGeneralContent(string content)
    {
        var sanitized = TrimNoisePrefix(content, aggressive: false);
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
        return sanitized;
    }

    private static string TrimNoisePrefix(string text, bool aggressive)
    {
        var trimmed = text.TrimStart();
        int removed = 0;

        while (trimmed.Length > 1 && removed < 2)
        {
            char first = trimmed[0];
            if (IsTrimmableNoise(first, aggressive))
            {
                trimmed = trimmed[1..].TrimStart();
                removed++;
                continue;
            }

            break;
        }

        return trimmed;
    }

    private static bool IsTrimmableNoise(char c, bool aggressive)
    {
        if (char.IsWhiteSpace(c)) return true;
        if (c is '0' or 'O' or 'o' or '○' or '●' or '·' or '•' or '．' or '.' or ',' or '，' or '、' or '：' or ':') return true;
        if (aggressive && c is '头' or '虐') return true;
        return false;
    }

}
