using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.App;

internal sealed class RealtimeWebhookPushCache
{
    private const string CriticalLogMarker = "任务";
    private const string ImportantNotificationMarker = "重要通知";
    private readonly WebhookDispatcher _dispatcher;
    private readonly TimeSpan _cacheDuration;
    private readonly List<WebhookMessage> _pendingMessages = [];
    private readonly SemaphoreSlim _sync = new(1, 1);
    private CancellationTokenSource? _flushDelayCancellation;

    public RealtimeWebhookPushCache(WebhookDispatcher dispatcher, int cacheSeconds)
    {
        _dispatcher = dispatcher;
        _cacheDuration = TimeSpan.FromSeconds(Math.Max(0, cacheSeconds));
    }

    public async Task SendAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Content))
        {
            return;
        }

        if (_cacheDuration <= TimeSpan.Zero)
        {
            await _dispatcher.SendAsync(message, cancellationToken);
            return;
        }

        if (IsCriticalLog(message.Content))
        {
            await FlushAsync(cancellationToken);
            await _dispatcher.SendAsync(message, cancellationToken);
            return;
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            _pendingMessages.Add(message);
            if (_pendingMessages.Count == 1)
            {
                StartFlushDelay();
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        List<WebhookMessage> messages;
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_pendingMessages.Count == 0)
            {
                return;
            }

            CancelFlushDelay();
            messages = _pendingMessages.ToList();
            _pendingMessages.Clear();
        }
        finally
        {
            _sync.Release();
        }

        var cachedTimeText = BuildCachedTimeText(messages);
        var cachedContent = string.Concat(messages.Select(FormatCachedContentLine));
        await _dispatcher.SendAsync(cachedContent, cachedTimeText, cancellationToken);
    }

    private static string BuildCachedTimeText(IReadOnlyList<WebhookMessage> messages)
        => $"{messages.First().TimeText}-{messages.Last().TimeText}";

    private static string FormatCachedContentLine(WebhookMessage message)
        => $"{Environment.NewLine}- {message.TimeText}: {message.Content.Replace("\r", " ").Replace("\n", " ").Trim()}";

    private void StartFlushDelay()
    {
        CancelFlushDelay();
        _flushDelayCancellation = new CancellationTokenSource();
        _ = FlushAfterDelayAsync(_flushDelayCancellation.Token);
    }

    private void CancelFlushDelay()
    {
        if (_flushDelayCancellation == null)
        {
            return;
        }

        _flushDelayCancellation.Cancel();
        _flushDelayCancellation.Dispose();
        _flushDelayCancellation = null;
    }

    private async Task FlushAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_cacheDuration, cancellationToken);
            await FlushAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.Warn($"Webhook 推送缓存自动刷新失败: {ex.Message}");
        }
    }

    private static bool IsCriticalLog(string content)
        => content.Contains(CriticalLogMarker, StringComparison.Ordinal)
            || content.Contains(ImportantNotificationMarker, StringComparison.Ordinal);
}
