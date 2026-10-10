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
    private static Bitmap ApplySharpen(Bitmap source)
    {
        int w = source.Width;
        int h = source.Height;
        var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);

        var srcData = source.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var dstData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int srcStride = srcData.Stride;
            int dstStride = dstData.Stride;
            var srcPixels = new byte[Math.Abs(srcStride) * h];
            var dstPixels = new byte[Math.Abs(dstStride) * h];

            Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dstOffset = y * dstStride + x * 4;
                    int srcOffset = y * srcStride + x * 4;

                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        Buffer.BlockCopy(srcPixels, srcOffset, dstPixels, dstOffset, 4);
                        continue;
                    }

                    for (int channel = 0; channel < 3; channel++)
                    {
                        int center = srcPixels[srcOffset + channel] * 5;
                        int left = srcPixels[y * srcStride + (x - 1) * 4 + channel];
                        int right = srcPixels[y * srcStride + (x + 1) * 4 + channel];
                        int top = srcPixels[(y - 1) * srcStride + x * 4 + channel];
                        int bottom = srcPixels[(y + 1) * srcStride + x * 4 + channel];
                        int value = center - left - right - top - bottom;
                        dstPixels[dstOffset + channel] = (byte)Math.Clamp(value, 0, 255);
                    }

                    dstPixels[dstOffset + 3] = srcPixels[srcOffset + 3];
                }
            }

            Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length);
        }
        finally
        {
            source.UnlockBits(srcData);
            result.UnlockBits(dstData);
        }

        return result;
    }

}
