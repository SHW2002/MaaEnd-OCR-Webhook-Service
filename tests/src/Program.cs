namespace MaaEnd_Log_Retransmitter.Tests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            MetricTests.Run();
            ReplaySamplerTests.Run();
            ReplaySegmentTests.Run();
            OrderedFilteringTests.Run();
            SegmentConcurrencyTests.RunAsync().GetAwaiter().GetResult();
            if (args.Length > 0 && args[0] == "--concurrency-only") return 0;
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            WindowMatchingTests.Run();
            if (args.Length > 0 && args[0] is "--gui-only" or "--sampling-only")
            {
                var directory = Path.Combine(AppContext.BaseDirectory, "verification");
                var recording = Directory.GetDirectories(directory, "session-*")
                    .Where(path => File.Exists(Path.Combine(path, "dataset.json")))
                    .OrderByDescending(Directory.GetLastWriteTimeUtc).First();
                var store = new Datasets.DatasetStore(recording);
                if (args[0] == "--sampling-only")
                    Task.Run(() => DatasetSamplingTests.RunAsync(store, directory)).GetAwaiter().GetResult();
                else
                {
                    GuiSmoke.Run(store, directory);
                    Console.WriteLine("PASS: GUI recording, browsing, and annotation layouts");
                }
                return 0;
            }
            return IntegrationTests.Run();
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    public static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
