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
    public static Rectangle? FindTextRect(List<OcrWordInfo> words, string searchText, bool caseSensitive)
    {
        if (words.Count == 0 || string.IsNullOrWhiteSpace(searchText)) return null;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var normalizedSearch = NormalizeCjkSpaces(searchText);

        var flatWords = new List<(int wordIdx, string text)>();
        for (int i = 0; i < words.Count; i++)
        {
            var norm = NormalizeCjkSpaces(words[i].Text);
            if (!string.IsNullOrEmpty(norm)) flatWords.Add((i, norm));
        }

        var fullNorm = string.Concat(flatWords.Select(w => w.text));
        var matchIdx = fullNorm.IndexOf(normalizedSearch, comparison);
        if (matchIdx < 0) return null;

        int matchEnd = matchIdx + normalizedSearch.Length;
        int runningPos = 0;
        int? firstWordIdx = null;
        int? lastWordIdx = null;

        foreach (var (wordIdx, wText) in flatWords)
        {
            int wordStart = runningPos;
            int wordEnd = runningPos + wText.Length;
            runningPos = wordEnd;

            if (wordStart < matchEnd && wordEnd > matchIdx)
            {
                firstWordIdx ??= wordIdx;
                lastWordIdx = wordIdx;
            }
        }

        if (firstWordIdx == null || lastWordIdx == null) return null;

        var first = words[firstWordIdx.Value];
        var last = words[lastWordIdx.Value];
        int x = first.X;
        int y = first.Y;
        int right = Math.Max(first.X + first.Width, last.X + last.Width);
        int bottom = Math.Max(first.Y + first.Height, last.Y + last.Height);
        return new Rectangle(x, y, right - x, bottom - y);
    }


    public static string NormalizeCjkSpaces(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ' || c == ' ')
            {
                bool prevCjk = i > 0 && IsCjkOrPunct(text[i - 1]);
                bool nextCjk = i < text.Length - 1 && IsCjkOrPunct(text[i + 1]);
                if (prevCjk || nextCjk) continue;
            }
            sb.Append(c);
        }

        return sb.ToString();
    }

    public static bool IsCjkOrPunct(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF)
            || (c >= 0x3400 && c <= 0x4DBF)
            || (c >= 0x20000 && c <= 0x2A6DF)
            || (c >= 0x3000 && c <= 0x303F)
            || (c >= 0xFF00 && c <= 0xFFEF)
            || (c >= 0x2F00 && c <= 0x2FDF)
            || (c >= 0x31C0 && c <= 0x31EF)
            || (c >= 0xFE30 && c <= 0xFE4F)
            || (c >= 0x3000 && c <= 0x301F)
            || c == '．' || c == '：' || c == '，' || c == '。'
            || c == '／' || c == '（' || c == '）' || c == '［'
            || c == '］' || c == '｛' || c == '｝';
    }

}
