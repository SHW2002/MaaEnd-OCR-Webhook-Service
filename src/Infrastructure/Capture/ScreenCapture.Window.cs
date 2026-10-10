using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MaaEnd_Log_Retransmitter.Infrastructure;

internal static partial class ScreenCapture
{
    public static Bitmap CaptureWindow(WindowInfo window)
        => CaptureWindow(window, out _);

    public static Bitmap CaptureWindow(WindowInfo window, out string method)
    {
        WindowFinder.Refresh(window);
        var rect = window.Rect;
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"窗口尺寸无效: {width}x{height}（窗口可能已最小化或不可见）");

        var bitmap = CaptureViaPrintWindow(window.Handle, width, height);
        if (bitmap != null)
        {
            method = "PrintWindow";
            Logger.Debug("截图方式: PrintWindow (直接窗口渲染)");
            return bitmap;
        }

        Logger.Debug("PrintWindow 失败, 尝试 DWM 缩略图 (后台捕获)...");
        bitmap = CaptureViaDwmThumbnail(window.Handle, width, height);
        if (bitmap != null)
        {
            method = "DWM-thumbnail";
            Logger.Debug("截图方式: DWM 缩略图 (后台合成)");
            return bitmap;
        }

        throw new InvalidOperationException("所有截图方式均失败（PrintWindow + DWM 缩略图）");
    }

    public static string SaveScreenshot(Bitmap bitmap, string windowTitle, int attempt)
    {
        var safeName = string.Join("_", windowTitle.Split(Path.GetInvalidFileNameChars()));
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
        Directory.CreateDirectory(dir);
        var filename = $"screenshot_{safeName}_{timestamp}_attempt{attempt}.png";
        var path = Path.Combine(dir, filename);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    private static Bitmap? CaptureViaPrintWindow(IntPtr hWnd, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        var hdc = g.GetHdc();
        try
        {
            if (PrintWindow(hWnd, hdc, PW_RENDERFULLCONTENT) || PrintWindow(hWnd, hdc, PW_CLIENTONLY))
            {
                return bitmap;
            }

            bitmap.Dispose();
            return null;
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }
    }

}
