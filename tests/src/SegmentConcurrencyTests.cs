using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class SegmentConcurrencyTests
{
    public static async Task RunAsync()
    {
        await VerifyOverlapAsync();
        await VerifyCancellationAsync(false);
        await VerifyCancellationAsync(true);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await ExpectCanceledAsync(SegmentExecutor.RunAsync<int, int>([0, 1], (_, _) =>
            throw new InvalidOperationException("已取消时不能启动识别"), canceled.Token));
        Console.WriteLine("PASS: segment worker overlap, chronological result order, failure cleanup, user cancellation");
    }

    private static async Task VerifyOverlapAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var allStarted = Signal();
        var releases = Enumerable.Range(0, 4).Select(_ => Signal()).ToArray();
        var lastFinished = Signal();
        var active = 0;
        var running = SegmentExecutor.RunAsync<int, int>([0, 1, 2, 3], async (segment, token) =>
        {
            if (Interlocked.Increment(ref active) == 4) allStarted.SetResult();
            try
            {
                await releases[segment].Task.WaitAsync(token);
                if (segment == 3) lastFinished.SetResult();
                return segment;
            }
            finally { Interlocked.Decrement(ref active); }
        }, timeout.Token);
        await allStarted.Task.WaitAsync(timeout.Token);
        Program.Assert(active == 4, "四个时间片段必须真正同时进行");
        releases[3].SetResult();
        await lastFinished.Task.WaitAsync(timeout.Token);
        Program.Assert(!running.IsCompleted, "较快片段完成后仍须等待其它片段");
        for (var i = 2; i >= 0; i--) releases[i].SetResult();
        var results = await running;
        Program.Assert(results.SequenceEqual(new[] { 0, 1, 2, 3 }), "完成顺序不能改变全局回放顺序");
        Program.Assert(active == 0, "执行返回前必须释放全部任务");
    }

    private static async Task VerifyCancellationAsync(bool failure)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var userCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var allStarted = Signal();
        var active = 0;
        var canceled = 0;
        var running = SegmentExecutor.RunAsync<int, int>([0, 1, 2], async (segment, token) =>
        {
            if (Interlocked.Increment(ref active) == 3) allStarted.SetResult();
            try
            {
                await allStarted.Task.WaitAsync(token);
                if (failure && segment == 2) throw new InvalidDataException("损坏输入");
                await Task.Delay(Timeout.Infinite, token);
                return segment;
            }
            catch (OperationCanceledException) { Interlocked.Increment(ref canceled); throw; }
            finally { Interlocked.Decrement(ref active); }
        }, userCancellation.Token);
        await allStarted.Task.WaitAsync(timeout.Token);
        if (failure)
        {
            try { await running; throw new InvalidOperationException("识别失败必须向上传递"); }
            catch (InvalidDataException ex) when (ex.Message == "损坏输入") { }
        }
        else
        {
            userCancellation.Cancel();
            await ExpectCanceledAsync(running);
        }
        Program.Assert(active == 0 && canceled == (failure ? 2 : 3), "失败或用户停止后必须等待所有工作任务结束");
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task ExpectCanceledAsync(Task running)
    {
        try { await running; }
        catch (OperationCanceledException) { return; }
        throw new InvalidOperationException("必须报告任务取消");
    }
}
