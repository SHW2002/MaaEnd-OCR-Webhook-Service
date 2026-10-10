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
    public class OcrWordInfo
    {
        public string Text { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class OcrResultData
    {
        public string Text { get; set; } = "";
        public List<OcrWordInfo> Words { get; set; } = new();
        public List<OcrLineInfo> Lines { get; set; } = new();
    }

    public class OcrLineInfo
    {
        private const string UnknownTimeText = "??:??:??";

        public string RawText { get; set; } = "";
        public string TimeText { get; set; } = "";
        public string Content { get; set; } = "";
        public List<OcrWordInfo> Words { get; set; } = new();

        public static string GetUnknownTimeText()
        {
            return UnknownTimeText;
        }

        public string GetDisplayTimeText()
        {
            return string.IsNullOrWhiteSpace(TimeText) ? UnknownTimeText : TimeText;
        }

        public string GetFinalContent()
        {
            return $"{GetDisplayTimeText()} {Content}";
        }
    }

    public enum OcrPreprocessMode
    {
        HighContrastBinary,
        DetailPreserving
    }

    private const int MaxProcessedTileWidth = 3200;
    private const int TileOverlap = 48;
    private static int _debugSeq;
}
