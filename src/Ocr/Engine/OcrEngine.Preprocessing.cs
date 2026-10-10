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
    private static Bitmap PreprocessForOcr(Bitmap source, int upscale, OcrPreprocessMode preprocessMode, bool saveDebugImages)
    {
        int newW = source.Width * upscale;
        int newH = source.Height * upscale;
        var debugDir = saveDebugImages ? EnsureDebugDir() : null;
        var modePrefix = preprocessMode == OcrPreprocessMode.HighContrastBinary ? "hc" : "detail";

        if (debugDir is not null) SaveDebugImage(source, debugDir, $"{modePrefix}_01_original");

        var scaled = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, newW, newH));
        }
        if (debugDir is not null) SaveDebugImage(scaled, debugDir, $"{modePrefix}_02_scaled");

        if (preprocessMode == OcrPreprocessMode.DetailPreserving)
        {
            var enhanced = ApplyColorContrast(scaled, 1.35f);
            scaled.Dispose();
            if (debugDir is not null) SaveDebugImage(enhanced, debugDir, $"{modePrefix}_03_color_contrast");

            var sharpened = ApplySharpen(enhanced);
            enhanced.Dispose();
            if (debugDir is not null) SaveDebugImage(sharpened, debugDir, $"{modePrefix}_04_sharpen");
            return sharpened;
        }

        var gray = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(gray))
        {
            var grayMatrix = new ColorMatrix(new float[][]
            {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });
            using var attr = new ImageAttributes();
            attr.SetColorMatrix(grayMatrix);
            g.DrawImage(scaled, new Rectangle(0, 0, newW, newH), 0, 0, newW, newH, GraphicsUnit.Pixel, attr);
        }
        scaled.Dispose();
        if (debugDir is not null) SaveDebugImage(gray, debugDir, $"{modePrefix}_03_grayscale");

        var contrasted = ApplyColorContrast(gray, 3.5f);
        gray.Dispose();
        if (debugDir is not null) SaveDebugImage(contrasted, debugDir, $"{modePrefix}_04_contrast");

        var binary = ApplyOtsuBinarization(contrasted);
        contrasted.Dispose();
        if (debugDir is not null) SaveDebugImage(binary, debugDir, $"{modePrefix}_05_binary");
        return binary;
    }

    private static Bitmap ApplyColorContrast(Bitmap source, float contrast)
    {
        int w = source.Width;
        int h = source.Height;
        var translate = (1.0f - contrast) * 0.5f;
        var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(result);
        using var attr = new ImageAttributes();
        var contrastMatrix = new ColorMatrix(new float[][]
        {
            new float[] { contrast, 0, 0, 0, 0 },
            new float[] { 0, contrast, 0, 0, 0 },
            new float[] { 0, 0, contrast, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { translate, translate, translate, 0, 1 }
        });
        attr.SetColorMatrix(contrastMatrix);
        g.DrawImage(source, new Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel, attr);
        return result;
    }


    private static string EnsureDebugDir()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void SaveDebugImage(Bitmap bmp, string dir, string label)
    {
        try
        {
            int seq = Interlocked.Increment(ref _debugSeq);
            var path = Path.Combine(dir, $"{seq:000}_{label}.png");
            bmp.Save(path, ImageFormat.Png);
        }
        catch
        {
        }
    }

}
