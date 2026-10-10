using System.Text;
using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Infrastructure;
using MaaEnd_Log_Retransmitter.Gui;

namespace MaaEnd_Log_Retransmitter;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var options = RuntimeOptions.Parse(args);
        Logger.SetDebugEnabled(options.Debug);
        if (args.Any(x => x.Equals("--cli", StringComparison.OrdinalIgnoreCase)))
        {
            ConsoleHost.Attach();
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            DpiAwareness.Enable();
            return AppRunner.RunAsync(options).GetAwaiter().GetResult();
        }
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(options.Debug));
        return 0;
    }
}
