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
    private static Bitmap ApplyOtsuBinarization(Bitmap source)
    {
        int w = source.Width, h = source.Height;
        var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

        var srcData = source.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int stride = srcData.Stride;
        int bytes = Math.Abs(stride) * h;
        var pixels = new byte[bytes];
        Marshal.Copy(srcData.Scan0, pixels, 0, bytes);
        source.UnlockBits(srcData);

        int[] hist = new int[256];
        var grayValues = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            int rowOff = y * stride;
            for (int x = 0; x < w; x++)
            {
                int px = rowOff + x * 4;
                byte gray = (byte)(pixels[px + 2] * 0.299 + pixels[px + 1] * 0.587 + pixels[px] * 0.114);
                grayValues[y * w + x] = gray;
                hist[gray]++;
            }
        }

        int total = w * h;
        int sumAll = 0;
        for (int i = 0; i < 256; i++) sumAll += i * hist[i];

        int threshold = 128;
        float maxVariance = 0;
        int weightBg = 0;
        long sumBg = 0;

        for (int t = 0; t < 256; t++)
        {
            weightBg += hist[t];
            if (weightBg == 0 || weightBg == total) continue;

            sumBg += (long)t * hist[t];
            int weightFg = total - weightBg;
            long sumFg = sumAll - sumBg;

            float meanBg = (float)sumBg / weightBg;
            float meanFg = (float)sumFg / weightFg;
            float variance = weightBg * weightFg * (meanBg - meanFg) * (meanBg - meanFg);

            if (variance > maxVariance)
            {
                maxVariance = variance;
                threshold = t;
            }
        }

        Logger.Debug($"Otsu 阈值: {threshold} (最大类间方差: {maxVariance:F0})");

        var dstData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        var dstPixels = new byte[Math.Abs(dstData.Stride) * h];

        for (int y = 0; y < h; y++)
        {
            int rowOff = y * dstData.Stride;
            for (int x = 0; x < w; x++)
            {
                byte gv = grayValues[y * w + x];
                byte val = gv < threshold ? (byte)0 : (byte)255;
                int px = rowOff + x * 4;
                dstPixels[px] = val;
                dstPixels[px + 1] = val;
                dstPixels[px + 2] = val;
                dstPixels[px + 3] = 255;
            }
        }

        Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length);
        result.UnlockBits(dstData);
        return result;
    }

}
