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
    private sealed class VisualLine
    {
        public List<OcrWordInfo> Words { get; } = new();
        public double CenterY { get; private set; }
        public double AverageHeight { get; private set; }

        public void Add(OcrWordInfo word)
        {
            Words.Add(word);
            double wordCenterY = word.Y + word.Height / 2.0;
            if (Words.Count == 1)
            {
                CenterY = wordCenterY;
                AverageHeight = word.Height;
                return;
            }

            int count = Words.Count;
            CenterY = ((CenterY * (count - 1)) + wordCenterY) / count;
            AverageHeight = ((AverageHeight * (count - 1)) + word.Height) / count;
        }
    }

    private static List<VisualLine> GroupWordsByVisualLines(List<OcrWordInfo> words)
    {
        if (words.Count == 0) return new List<VisualLine>();

        var sorted = words
            .OrderBy(word => word.Y + word.Height / 2.0)
            .ThenBy(word => word.X)
            .ToList();

        var lines = new List<VisualLine>();
        foreach (var word in sorted)
        {
            var line = lines.FirstOrDefault(candidate => IsSameVisualLine(candidate, word));
            if (line == null)
            {
                line = new VisualLine();
                lines.Add(line);
            }

            line.Add(word);
        }

        return lines.OrderBy(line => line.CenterY).ToList();
    }

    private static bool IsSameVisualLine(VisualLine line, OcrWordInfo word)
    {
        double wordCenterY = word.Y + word.Height / 2.0;
        double tolerance = Math.Max(line.AverageHeight, word.Height) * 0.6;
        return Math.Abs(line.CenterY - wordCenterY) <= tolerance;
    }

    private static List<OcrLineInfo> BuildLineInfos(List<VisualLine> visualLines)
    {
        var result = new List<OcrLineInfo>();
        foreach (var visualLine in visualLines)
        {
            var orderedWords = visualLine.Words.OrderBy(word => word.X).ToList();
            if (orderedWords.Count == 0) continue;

            var rawText = NormalizeOcrSpacing(BuildLineText(orderedWords));
            var split = TrySplitTimeAndContent(orderedWords);
            var content = split?.Content ?? rawText;
            var normalizedContent = NormalizeStructuredContent(SanitizeContentPrefix(NormalizeOcrSpacing(content)));

            result.Add(new OcrLineInfo
            {
                RawText = rawText,
                TimeText = split?.TimeText ?? string.Empty,
                Content = normalizedContent,
                Words = orderedWords
            });
        }

        return MergeContinuationLines(result);
    }


    private static int GetFirstWordX(List<OcrWordInfo> words)
    {
        return words.Count == 0 ? -1 : words.Min(word => word.X);
    }

    private static int GetMinTop(List<OcrWordInfo> words)
    {
        return words.Count == 0 ? -1 : words.Min(word => word.Y);
    }

    private static int GetMaxBottom(List<OcrWordInfo> words)
    {
        return words.Count == 0 ? -1 : words.Max(word => word.Y + word.Height);
    }

    private static double GetAverageHeight(IEnumerable<OcrWordInfo> words)
    {
        var heights = words.Select(word => word.Height).Where(height => height > 0).ToList();
        return heights.Count == 0 ? 0 : heights.Average();
    }

}
