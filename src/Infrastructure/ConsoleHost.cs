using System.Runtime.InteropServices;

namespace MaaEnd_Log_Retransmitter.Infrastructure;

internal static class ConsoleHost
{
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(uint processId);
    [DllImport("kernel32.dll")] private static extern bool AllocConsole();
    public static void Attach() { if (!AttachConsole(uint.MaxValue)) AllocConsole(); }
}
