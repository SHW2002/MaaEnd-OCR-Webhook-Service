using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.App;

internal static partial class CliConfiguration
{
    private static bool TryAskRequiredString(string prompt, string defaultValue, out string value)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                value = defaultValue;
                return false;
            }

            value = string.IsNullOrWhiteSpace(input) ? defaultValue : input;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            Logger.Warn("输入无效, 该配置不能为空");
        }
    }

    private static int GetIntegerDefault(int value, int fallback, int min, int max)
        => value >= min && value <= max ? value : fallback;

    private static string GetRequiredStringDefault(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static Arguments? InteractivePrompt()
    {
        var config = new Arguments();

        if (!TryAskRequiredString($"请输入窗口标题 (默认: {config.WindowTitle}, 输入 q 退出): ", config.WindowTitle, out var title)) return null;
        config.WindowTitle = title;

        Console.Write("请输入要查找的文本 (必填, 输入 q 退出): ");
        var input = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(input)) { Logger.Error("搜索文本不能为空"); return null; }
        if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) return null;
        config.SearchText = input;

        config.PartialMatch = AskYesNo("是否使用模糊匹配? (y/n, 默认: n 精确匹配): ", false);
        config.WindowTitleCaseSensitive = AskYesNo("窗口标题是否区分大小写? (y/n, 默认: n 不区分): ", false);
        config.CaseSensitive = AskYesNo("日志区域锚点是否区分大小写? (y/n, 默认: n 不区分): ", false);

        Console.Write($"请输入 OCR 语言标签 (默认: {config.Language}): ");
        input = Console.ReadLine()?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(input)) config.Language = input;

        config.Retry = AskInteger("请输入重试次数 (1-10, 默认: 1): ", 1, 1, 10);
        config.RetryInterval = AskInteger("请输入重试间隔/毫秒 (100-60000, 默认: 1000): ", 1000, 100, 60000);
        config.RollingIntervalMs = AskInteger("请输入滚动识别间隔/毫秒 (500-60000, 默认: 3000): ", 3000, 500, 60000);
        config.WebhookUrl = AskWebhookUrl(config.WebhookUrl);

        config.WebhookBody = AskWebhookBody(config.WebhookBody);

        config.WebhookContentType = AskRequiredString($"请输入 Webhook Content-Type (默认: {config.WebhookContentType}): ", config.WebhookContentType);

        config.WebhookTimeoutMs = AskInteger("请输入 Webhook 超时/毫秒 (1000-60000, 默认: 5000): ", 5000, 1000, 60000);
        config.WebhookMode = AskWebhookMode();
        config.WebhookPushCacheSeconds = AskInteger("请输入 Webhook 推送缓存时间/秒 (0-86400, 0=不启用, 默认: 0): ", 0, 0, 86400);

        if (!config.TryValidate(out var validationErrors))
        {
            foreach (var error in validationErrors)
            {
                Logger.Error($"配置错误: {error}");
            }

            return null;
        }

        PrintSummary(config);
        if (!AskYesNo("确认以上配置? (y/n, 默认: y): ", true))
        {
            Logger.Info("已取消");
            return null;
        }

        return config;
    }

    private static bool AskYesNo(string prompt, bool defaultYes)
    {
        var suffix = defaultYes ? " [Y/n]" : " [y/N]";
        Console.Write(prompt + suffix);
        var input = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(input)) return defaultYes;
        return input == "y" || input == "yes";
    }

    private static int AskInteger(string prompt, int defaultValue, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            if (int.TryParse(input, out var value) && value >= min && value <= max) return value;
            Logger.Warn($"输入无效, 请输入 {min} 到 {max} 之间的整数");
        }
    }

    private static string AskWebhookUrl(string defaultValue)
    {
        while (true)
        {
            Console.Write($"请输入 Webhook URL (必填, 默认: {defaultValue}): ");
            var input = Console.ReadLine()?.Trim() ?? "";
            var value = string.IsNullOrWhiteSpace(input) ? defaultValue : input;
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }

            Logger.Warn("输入无效, Webhook URL 必须是合法的 http/https URL");
        }
    }

    private static string AskWebhookBody(string defaultValue)
    {
        while (true)
        {
            var path = PrepareWebhookBodyTemplateFile(defaultValue);
            Logger.Info($"即将打开 Webhook Body 模板文件: {path}");
            Logger.Info("请在记事本中编辑模板，保存并关闭记事本后继续。模板必须包含 __CONTENT__，可选包含 __TIME__。");

            if (!OpenWebhookBodyTemplateEditor(path))
            {
                Console.WriteLine($"无法自动打开记事本，请手动编辑文件后按 Enter 继续: {path}");
                Console.ReadLine();
            }

            var value = ReadWebhookBodyTemplateFile(path);

            if (!string.IsNullOrWhiteSpace(value) && value.Contains("__CONTENT__", StringComparison.Ordinal))
            {
                return value;
            }

            Logger.Warn("Webhook Body 模板无效，不能为空且必须包含 __CONTENT__。将重新打开模板文件。");
        }
    }

}
