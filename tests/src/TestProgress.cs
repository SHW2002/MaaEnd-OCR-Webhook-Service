namespace MaaEnd_Log_Retransmitter.Tests;

internal sealed class TestProgress(Action<string> callback) : IProgress<string>
{
    public void Report(string value) => callback(value);
}

internal sealed class TestProgress<T>(Action<T> callback) : IProgress<T>
{
    public void Report(T value) => callback(value);
}
