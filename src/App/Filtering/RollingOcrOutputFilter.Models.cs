using System.Text;
using System.Text.RegularExpressions;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.App;

internal sealed partial class RollingOcrOutputFilter
{
    internal sealed class Result
    {
        private Result(
            bool accepted,
            IReadOnlyList<OcrEngine.OcrLineInfo> currentLines,
            IReadOnlyList<OutputLine> newLines,
            string? rejectionReason)
        {
            Accepted = accepted;
            CurrentLines = currentLines;
            NewLines = newLines;
            RejectionReason = rejectionReason;
        }

        public bool Accepted { get; }
        public IReadOnlyList<OcrEngine.OcrLineInfo> CurrentLines { get; }
        public IReadOnlyList<OutputLine> NewLines { get; }
        public string? RejectionReason { get; }

        public static Result Accept(
            IReadOnlyList<OcrEngine.OcrLineInfo> currentLines,
            IReadOnlyList<OutputLine> newLines)
        {
            return new Result(true, currentLines, newLines, null);
        }

        public static Result Reject(IReadOnlyList<OcrEngine.OcrLineInfo> currentLines, string rejectionReason)
        {
            return new Result(false, currentLines, Array.Empty<OutputLine>(), rejectionReason);
        }
    }

    internal sealed class OutputLine(
        string timeText,
        string content,
        LineCategory category,
        string canonicalContent,
        string signature,
        OcrEngine.OcrLineInfo source)
    {
        public string TimeText { get; } = timeText;
        public string Content { get; } = content;
        public LineCategory Category { get; } = category;
        public string CanonicalContent { get; } = canonicalContent;
        public string Signature { get; } = signature;
        public OcrEngine.OcrLineInfo Source { get; } = source;
    }

    internal enum LineCategory
    {
        TaskStarted,
        TaskCompleted,
        TaskFailed,
        Activity,
        Other
    }
}
