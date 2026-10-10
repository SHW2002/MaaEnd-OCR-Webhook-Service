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
    public static async Task<OcrResultData> RecognizeWithWordsAsync(
        Bitmap bitmap,
        string languageTag,
        int upscale,
        OcrPreprocessMode preprocessMode = OcrPreprocessMode.HighContrastBinary,
        bool saveDebugImages = false)
    {
        if (ShouldUseTiledRecognition(bitmap, upscale))
        {
            return await RecognizeTiledWithWordsAsync(bitmap, languageTag, upscale, preprocessMode, saveDebugImages);
        }

        return await RecognizeSingleWithWordsAsync(bitmap, languageTag, upscale, preprocessMode, saveDebugImages);
    }

    private static bool ShouldUseTiledRecognition(Bitmap bitmap, int upscale)
        => bitmap.Width * upscale > MaxProcessedTileWidth;

    private static async Task<OcrResultData> RecognizeSingleWithWordsAsync(
        Bitmap bitmap,
        string languageTag,
        int upscale,
        OcrPreprocessMode preprocessMode,
        bool saveDebugImages)
    {
        using var processed = PreprocessForOcr(bitmap, upscale, preprocessMode, saveDebugImages);
        Logger.Debug($"图像预处理 ({preprocessMode}, {upscale}x): {bitmap.Width}x{bitmap.Height} → {processed.Width}x{processed.Height}");

        using var softwareBitmap = await ConvertToSoftwareBitmapAsync(processed);
        var engine = GetEngine(languageTag);
        var result = await engine.RecognizeAsync(softwareBitmap);

        var words = new List<OcrWordInfo>();
        foreach (var line in result.Lines)
        {
            foreach (var word in line.Words)
            {
                var rect = word.BoundingRect;
                words.Add(new OcrWordInfo
                {
                    Text = word.Text,
                    X = (int)rect.X / upscale,
                    Y = (int)rect.Y / upscale,
                    Width = (int)rect.Width / upscale,
                    Height = (int)rect.Height / upscale
                });
            }
        }

        return BuildResultFromWords(words);
    }

    private static async Task<OcrResultData> RecognizeTiledWithWordsAsync(
        Bitmap bitmap,
        string languageTag,
        int upscale,
        OcrPreprocessMode preprocessMode,
        bool saveDebugImages)
    {
        int tileWidth = Math.Max(1, MaxProcessedTileWidth / Math.Max(1, upscale));
        int overlap = Math.Min(TileOverlap, Math.Max(0, tileWidth / 4));
        int step = Math.Max(1, tileWidth - overlap);
        var allWords = new List<OcrWordInfo>();

        Logger.Debug($"OCR tiled recognition: {bitmap.Width}x{bitmap.Height}, tileWidth={tileWidth}, overlap={overlap}, upscale={upscale}x");

        for (int left = 0, tileIndex = 1; left < bitmap.Width; left += step, tileIndex++)
        {
            int width = Math.Min(tileWidth, bitmap.Width - left);
            if (width <= 0) break;

            var tileRect = new Rectangle(left, 0, width, bitmap.Height);
            using var tile = bitmap.Clone(tileRect, bitmap.PixelFormat);
            var tileResult = await RecognizeSingleWithWordsAsync(tile, languageTag, upscale, preprocessMode, saveDebugImages);

            int coreLeft = left == 0 ? left : left + overlap / 2;
            int coreRight = left + width >= bitmap.Width ? left + width : left + width - overlap / 2;
            int accepted = 0;

            foreach (var word in tileResult.Words)
            {
                int globalCenterX = left + word.X + word.Width / 2;
                if (globalCenterX < coreLeft || globalCenterX >= coreRight)
                {
                    continue;
                }

                allWords.Add(new OcrWordInfo
                {
                    Text = word.Text,
                    X = left + word.X,
                    Y = word.Y,
                    Width = word.Width,
                    Height = word.Height
                });
                accepted++;
            }

            Logger.Debug($"OCR tile {tileIndex}: x={left}, width={width}, acceptedWords={accepted}");

            if (left + width >= bitmap.Width) break;
        }

        return BuildResultFromWords(allWords);
    }

    private static OcrResultData BuildResultFromWords(List<OcrWordInfo> words)
    {
        var visualLines = GroupWordsByVisualLines(words);
        var orderedWords = visualLines
            .SelectMany(line => line.Words.OrderBy(word => word.X))
            .ToList();
        var parsedLines = BuildLineInfos(visualLines);
        var text = string.Join(Environment.NewLine, parsedLines
            .Select(line => line.RawText)
            .Where(line => !string.IsNullOrWhiteSpace(line)));

        return new OcrResultData
        {
            Text = text,
            Words = orderedWords,
            Lines = parsedLines
        };
    }


    private static Windows.Media.Ocr.OcrEngine GetEngine(string languageTag)
    {
        var requestedLanguage = new Language(languageTag);
        if (Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(requestedLanguage) is { } engine)
        {
            Logger.Debug($"使用 OCR 语言: {languageTag}");
            return engine;
        }

        var available = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages;
        if (available.Count == 0)
            throw new NotSupportedException("系统没有可用的 OCR 语言包");

        var fallback = available[0];
        var fallbackEngine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(fallback);
        if (fallbackEngine == null)
            throw new NotSupportedException($"无法创建 OCR 引擎（语言: {fallback.DisplayName}）");

        Logger.Warn($"不支持语言 \"{languageTag}\", 已回退到: {fallback.DisplayName} ({fallback.LanguageTag})");
        return fallbackEngine;
    }


    private static async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Bmp);
        ms.Position = 0;

        var randomAccessStream = ms.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Rgba8 && softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Gray8)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Rgba8);
        }

        return softwareBitmap;
    }
}
