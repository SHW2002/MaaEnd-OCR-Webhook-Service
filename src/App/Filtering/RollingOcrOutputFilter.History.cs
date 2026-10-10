using System.Text;
using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.App;

internal sealed partial class RollingOcrOutputFilter
{
    private static readonly string[] StructuredCollapseMarkers =
    [
        "任务开始",
        "任务完成",
        "任务失败",
        "正在检查画面"
    ];

    private static readonly Regex LeadingTimePattern = new(
        @"^\s*(?<time>[0-9０-９:：﹕︰．.·•,，\-]{4,20})\s*(?<content>\S.+)$",
        RegexOptions.Compiled);

    private static readonly Regex StructuredEventPattern = new(
        @"^(?<prefix>任务开始|任务完成|任务失败)\s*(?:[:：．.]|\s)?\s*(?<payload>.+)$",
        RegexOptions.Compiled);

    private static readonly Regex LeadingTimeOcrPattern = new(
        @"^\s*(?<time>[0-9oOcCeE\uFF10-\uFF19:\uFF1A\uFE55\uA789.\u00B7\u2022,\uFF0C-]{4,20})\s*(?<content>\S.+)$",
        RegexOptions.Compiled);

    private const int MaxHistorySize = 160;
    private readonly List<string> _acceptedSignatures = [];
    private List<string>? _previousFrameSignatures;

    public Result Filter(List<OcrEngine.OcrLineInfo> lines)
    {
        var currentLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line.RawText))
            .ToList();

        if (IsUnstableFrame(currentLines, out var rejectionReason))
        {
            return Result.Reject(currentLines, rejectionReason);
        }

        var normalizedLines = currentLines
            .Select(TryBuildOutputLine)
            .Where(line => line != null)
            .Cast<OutputLine>()
            .DistinctBy(line => line.Signature)
            .ToList();

        var visibleSlice = TrimFrameOverlap(normalizedLines);
        var newLines = visibleSlice
            .Where(line => !_acceptedSignatures.Contains(line.Signature, StringComparer.Ordinal))
            .ToList();

        foreach (var line in newLines)
        {
            _acceptedSignatures.Add(line.Signature);
        }

        if (_acceptedSignatures.Count > MaxHistorySize)
        {
            _acceptedSignatures.RemoveRange(0, _acceptedSignatures.Count - MaxHistorySize);
        }

        _previousFrameSignatures = normalizedLines.Select(line => line.Signature).ToList();
        return Result.Accept(currentLines, newLines);
    }

    private bool IsUnstableFrame(List<OcrEngine.OcrLineInfo> currentLines, out string reason)
    {
        reason = string.Empty;

        if (_previousFrameSignatures == null || _previousFrameSignatures.Count < 2) return false;
        if (currentLines.Count != 1) return false;

        var onlyLine = currentLines[0];
        if (!string.IsNullOrWhiteSpace(onlyLine.TimeText)) return false;

        var content = NormalizeText(onlyLine.Content);
        if (content.Length < 80) return false;

        int structuredMarkerCount = StructuredCollapseMarkers.Sum(marker => CountOccurrences(content, marker));
        if (structuredMarkerCount < 2) return false;

        reason = $"单行内容疑似塌缩，且命中 {structuredMarkerCount} 个结构化标记";
        return true;
    }

    private List<OutputLine> TrimFrameOverlap(List<OutputLine> currentLines)
    {
        if (_previousFrameSignatures == null || _previousFrameSignatures.Count == 0)
        {
            return currentLines;
        }

        var currentSignatures = currentLines.Select(line => line.Signature).ToList();
        int maxOverlap = Math.Min(_previousFrameSignatures.Count, currentSignatures.Count);
        for (int overlap = maxOverlap; overlap >= 1; overlap--)
        {
            bool matched = true;
            for (int i = 0; i < overlap; i++)
            {
                if (!string.Equals(_previousFrameSignatures[_previousFrameSignatures.Count - overlap + i], currentSignatures[i], StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return currentLines.Skip(overlap).ToList();
            }
        }

        var previousSet = new HashSet<string>(_previousFrameSignatures, StringComparer.Ordinal);
        return currentLines
            .Where(line => !previousSet.Contains(line.Signature))
            .ToList();
    }

}
