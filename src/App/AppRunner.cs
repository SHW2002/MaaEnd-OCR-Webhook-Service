using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.App;

internal static class AppRunner
{
    public static async Task<int> RunAsync(RuntimeOptions options)
    {
        var config = CliConfiguration.LoadOrCreateConfig(ConfigStore.Path, options);
        if (config == null) return 0;
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            await LiveRecognitionRunner.RunAsync(config, options.Debug, cancellation.Token);
            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return 0; }
        catch (Exception ex) { Logger.Error(ex.Message); return 2; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
