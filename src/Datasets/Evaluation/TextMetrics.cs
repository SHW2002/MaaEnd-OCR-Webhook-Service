using System.Text;
using System.Text.RegularExpressions;

namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class TextMetrics
{
    public static string Normalize(string text, bool normalizeLayout)
        => normalizeLayout ? Regex.Replace(text.Normalize(NormalizationForm.FormKC), @"\s+", "") : text;

    public static int Distance(string expected, string actual)
    {
        var a = expected.EnumerateRunes().ToArray();
        var b = actual.EnumerateRunes().ToArray();
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        var current = new int[b.Length + 1];
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1),
                    previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
    public static int CharacterCount(string text) => text.EnumerateRunes().Count();

    // Sequence alignment keeps an omitted line from shifting every subsequent timestamp comparison.
    public static List<(TruthLine Expected, TruthLine? Actual)> Align(
        IReadOnlyList<TruthLine> expected, IReadOnlyList<TruthLine> actual, bool normalized)
    {
        var a = expected.Select(x => Normalize(x.Content, normalized)).ToArray();
        var b = actual.Select(x => Normalize(x.Content, normalized)).ToArray();
        var costs = new int[a.Length + 1, b.Length + 1];
        for (var i = 1; i <= a.Length; i++) costs[i, 0] = costs[i - 1, 0] + Math.Max(1, a[i - 1].Length);
        for (var j = 1; j <= b.Length; j++) costs[0, j] = costs[0, j - 1] + Math.Max(1, b[j - 1].Length);
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                costs[i, j] = Math.Min(costs[i - 1, j - 1] + Distance(a[i - 1], b[j - 1]),
                    Math.Min(costs[i - 1, j] + Math.Max(1, a[i - 1].Length), costs[i, j - 1] + Math.Max(1, b[j - 1].Length)));
        var pairs = new List<(TruthLine, TruthLine?)>();
        int row = a.Length, column = b.Length;
        while (row > 0 || column > 0)
        {
            if (row > 0 && column > 0 && costs[row, column] == costs[row - 1, column - 1] + Distance(a[row - 1], b[column - 1]))
            { pairs.Add((expected[--row], actual[--column])); }
            else if (row > 0 && costs[row, column] == costs[row - 1, column] + Math.Max(1, a[row - 1].Length))
            { pairs.Add((expected[--row], null)); }
            else column--;
        }
        pairs.Reverse();
        return pairs;
    }
}
