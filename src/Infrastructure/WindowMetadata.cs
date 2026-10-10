using System.Runtime.InteropServices;

namespace MaaEnd_Log_Retransmitter.Infrastructure;

internal static class WindowMetadata
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
    public static uint GetDpi(WindowInfo window) => GetDpiForWindow(window.Handle);
}
