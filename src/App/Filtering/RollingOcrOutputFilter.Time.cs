using System.Text;
using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.App;

internal sealed partial class RollingOcrOutputFilter
{
    private static bool TryExtractLeadingTime(string text, out string timeText, out string content)
    {
        timeText = string.Empty;
        content = string.Empty;

        if (string.IsNullOrWhiteSpace(text)) return false;

        var match = LeadingTimeOcrPattern.Match(text);
        if (!match.Success) return false;

        var normalizedTime = NormalizeTimeText(match.Groups["time"].Value);
        if (string.IsNullOrWhiteSpace(normalizedTime)) return false;

        timeText = normalizedTime;
        content = match.Groups["content"].Value.Trim();
        return !string.IsNullOrWhiteSpace(content);
    }

    private static string NormalizeTimeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = NormalizeText(text);
        var parts = Regex.Split(normalized, "[：:﹕︰．.·•,，]+")
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        if (parts.Count >= 2)
        {
            var hh = NormalizeTimeComponent(parts.ElementAtOrDefault(0));
            var mm = NormalizeTimeComponent(parts.ElementAtOrDefault(1));
            var ss = NormalizeTimeComponent(parts.ElementAtOrDefault(2));
            return $"{hh}:{mm}:{ss}";
        }

        var flatChars = normalized
            .Where(c => !char.IsWhiteSpace(c) && !IsTimeSeparator(c))
            .Select(c => TryNormalizeTimeDigit(c, out var digit) ? digit : '?')
            .ToList();

        if (flatChars.Count == 0) return string.Empty;
        while (flatChars.Count < 6)
        {
            flatChars.Add('?');
        }

        return $"{flatChars[0]}{flatChars[1]}:{flatChars[2]}{flatChars[3]}:{flatChars[4]}{flatChars[5]}";
    }

    private static string NormalizeTimeComponent(string? part)
    {
        if (string.IsNullOrWhiteSpace(part)) return "??";

        var chars = part
            .Where(c => !char.IsWhiteSpace(c))
            .Select(c => TryNormalizeTimeDigit(c, out var digit) ? digit : '?')
            .Take(2)
            .ToList();

        while (chars.Count < 2)
        {
            chars.Add('?');
        }

        return new string(chars.ToArray());
    }

    private static string? ExtractLooseTime(string text)
    {
        var match = LeadingTimeOcrPattern.Match(text);
        if (!match.Success) return null;
        var candidate = NormalizeTimeText(match.Groups["time"].Value);
        return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
    }


    private static bool TryNormalizeDigit(char c, out char digit)
    {
        if (c >= '0' && c <= '9')
        {
            digit = c;
            return true;
        }

        if (c >= '０' && c <= '９')
        {
            digit = (char)('0' + (c - '０'));
            return true;
        }

        digit = default;
        return false;
    }

    private static bool TryNormalizeTimeDigit(char c, out char digit)
    {
        if (TryNormalizeDigit(c, out digit))
        {
            return true;
        }

        if (c is 'o' or 'O' or 'c' or 'C' or 'e' or 'E')
        {
            digit = '0';
            return true;
        }

        return false;
    }

    private static char NormalizeDigitChar(char c)
    {
        return TryNormalizeDigit(c, out var digit) ? digit : c;
    }

    private static bool IsTimeSeparator(char c)
    {
        return c is ':' or '：' or '﹕' or '︰' or '．' or '.' or '·' or '•' or ',' or '，' or '-';
    }

    private static bool IsValidHourMinute(string hourText, string minuteText)
    {
        if (!int.TryParse(hourText, out var hour) || hour is < 0 or > 23) return false;
        return int.TryParse(minuteText, out var minute) && minute is >= 0 and <= 59;
    }

    private static bool IsValidTimeComponents(string hourText, string minuteText, string secondText)
    {
        if (!IsValidHourMinute(hourText, minuteText)) return false;
        return int.TryParse(secondText, out var second) && second is >= 0 and <= 59;
    }

}
