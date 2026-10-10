using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MaaEnd_Log_Retransmitter.Infrastructure;

internal static partial class ScreenCapture
{
    private static Bitmap? CaptureViaDwmThumbnail(IntPtr targetHwnd, int width, int height)
    {
        IntPtr hThumbnail = IntPtr.Zero;
        IntPtr hHostWnd = IntPtr.Zero;

        try
        {
            RegisterHostWindowClass();

            hHostWnd = CreateWindowEx(
                WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
                "OcrHostWindow",
                "",
                WS_POPUP,
                0,
                0,
                width,
                height,
                IntPtr.Zero,
                IntPtr.Zero,
                GetModuleHandle(null!),
                IntPtr.Zero);

            if (hHostWnd == IntPtr.Zero) return null;

            SetLayeredWindowAttributes(hHostWnd, 0, 1, LWA_ALPHA);
            ShowWindow(hHostWnd, SW_SHOWNOACTIVATE);

            int hr = DwmRegisterThumbnail(hHostWnd, targetHwnd, out hThumbnail);
            if (hr != 0 || hThumbnail == IntPtr.Zero) return null;

            DwmQueryThumbnailSourceSize(hThumbnail, out var srcSize);
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTSOURCE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY,
                fVisible = true,
                fSourceClientAreaOnly = false,
                opacity = 255,
                rcSource = new RECT { Left = 0, Top = 0, Right = srcSize.cx, Bottom = srcSize.cy },
                rcDestination = new RECT { Left = 0, Top = 0, Right = srcSize.cx, Bottom = srcSize.cy }
            };
            DwmUpdateThumbnailProperties(hThumbnail, ref props);

            DwmFlush();
            Thread.Sleep(50);

            var bitmap = new Bitmap(srcSize.cx, srcSize.cy, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bitmap);
            var hdc = g.GetHdc();
            try
            {
                if (!PrintWindow(hHostWnd, hdc, PW_RENDERFULLCONTENT))
                {
                    bitmap.Dispose();
                    return null;
                }
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            Logger.Debug($"DWM 缩略图捕获异常: {ex.Message}");
            return null;
        }
        finally
        {
            if (hThumbnail != IntPtr.Zero) DwmUnregisterThumbnail(hThumbnail);
            if (hHostWnd != IntPtr.Zero) DestroyWindow(hHostWnd);
        }
    }

    private static void RegisterHostWindowClass()
    {
        if (_windowClassRegistered)
        {
            return;
        }

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = 0,
            lpfnWndProc = HostWndProc,
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = GetModuleHandle(null!),
            hIcon = IntPtr.Zero,
            hCursor = IntPtr.Zero,
            hbrBackground = IntPtr.Zero,
            lpszMenuName = "",
            lpszClassName = "OcrHostWindow",
            hIconSm = IntPtr.Zero
        };

        if (RegisterClassEx(ref wc) == 0)
            throw new InvalidOperationException("注册宿主窗口类失败");

        _windowClassRegistered = true;
    }

    private static IntPtr HostWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_DESTROY) return IntPtr.Zero;
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
