using System.Text.Json;

namespace MaaEnd_Log_Retransmitter.App;

internal static class ConfigStore
{
    public static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, "config.json");
    public static JsonSerializerOptions JsonOptions { get; } = new() { WriteIndented = true };
    public static Arguments Load()
        => File.Exists(Path)
            ? JsonSerializer.Deserialize<Arguments>(File.ReadAllText(Path)) ?? throw new InvalidDataException("配置为空")
            : new Arguments { SearchText = "运行日志" };
    public static void Save(Arguments config)
        => File.WriteAllText(Path, JsonSerializer.Serialize(config, JsonOptions));
    public static Arguments RecognitionSnapshot(Arguments config) => new()
    {
        WindowTitle = config.WindowTitle, SearchText = config.SearchText, PartialMatch = config.PartialMatch,
        WindowTitleCaseSensitive = config.WindowTitleCaseSensitive,
        CaseSensitive = config.CaseSensitive, Language = config.Language, Retry = config.Retry,
        RetryInterval = config.RetryInterval, RollingIntervalMs = config.RollingIntervalMs
    };
}
