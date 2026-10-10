using MaaEnd_Log_Retransmitter.Infrastructure;
using MaaEnd_Log_Retransmitter.Ocr;

namespace MaaEnd_Log_Retransmitter.App;

internal static class LiveRecognitionRunner
{
    public static async Task RunAsync(Arguments config, bool debug, CancellationToken token)
    {
        if (!config.TryValidate(true, out var errors)) throw new ArgumentException(string.Join("\n", errors));
        var window = WindowFinder.FindWindow(config.WindowTitle, config.PartialMatch, config.WindowTitleCaseSensitive)
            ?? throw new InvalidOperationException("未找到目标窗口，请检查窗口标题");
        var pipeline = new FrameRecognitionPipeline(config, debug);
        var dispatcher = new WebhookDispatcher(config);
        var cache = new RealtimeWebhookPushCache(dispatcher, config.WebhookPushCacheSeconds);
        var buffered = new List<OcrEngine.OcrLineInfo>();
        var failures = 0;
        Logger.Info($"开始识别：{window.Title}");
        try
        {
            while (!token.IsCancellationRequested)
            {
                using var bitmap = ScreenCapture.CaptureWindow(window);
                if (debug) ScreenCapture.SaveScreenshot(bitmap, window.Title, Environment.TickCount);
                var result = await pipeline.ProcessAsync(bitmap, token);
                if (!result.AnchorFound)
                {
                    Logger.Warn(result.Error ?? "锚点识别失败");
                    if (++failures >= config.Retry) throw new InvalidOperationException("已达到最大锚点识别重试次数");
                }
                else
                {
                    failures = 0;
                    Logger.Debug($"本轮完整处理耗时 {result.TotalMilliseconds:F1}ms，候选 {result.Recognition?.Selected.Spec.Name}");
                    if (!result.Accepted) Logger.Warn(result.Error ?? "本轮结果被过滤");
                    foreach (var line in result.NewLines)
                    {
                        buffered.Add(line.Source);
                        Logger.Info(line.Source.GetFinalContent());
                        if (config.HasWebhookUrl && config.ShouldPushRealtime)
                            await cache.SendAsync(WebhookMessage.FromOcrLine(line.Source), CancellationToken.None);
                    }
                }
                await Task.Delay(FrameRecognitionPipeline.NextDelay(result, config), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            if (config.HasWebhookUrl && config.ShouldPushRealtime) await cache.FlushAsync(CancellationToken.None);
            if (config.HasWebhookUrl && config.ShouldPushSummary)
                foreach (var line in buffered) await dispatcher.SendAsync(WebhookMessage.FromOcrLine(line), CancellationToken.None);
            Logger.Info("识别已停止");
        }
    }
}
