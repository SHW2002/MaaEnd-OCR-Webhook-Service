namespace MaaEnd_Log_Retransmitter.Datasets;

internal static class SegmentExecutor
{
    public static async Task<TResult[]> RunAsync<TInput, TResult>(IReadOnlyList<TInput> segments,
        Func<TInput, CancellationToken, Task<TResult>> run, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        async Task<TResult> RunOneAsync(TInput segment)
        {
            try
            {
                cancellation.Token.ThrowIfCancellationRequested();
                return await run(segment, cancellation.Token);
            }
            catch
            {
                cancellation.Cancel();
                throw;
            }
        }
        // One worker per nonempty time segment; results retain chronological segment order.
        var tasks = segments.Select(segment => Task.Run(() => RunOneAsync(segment))).ToArray();
        var results = await Task.WhenAll(tasks);
        token.ThrowIfCancellationRequested();
        return results;
    }
}
