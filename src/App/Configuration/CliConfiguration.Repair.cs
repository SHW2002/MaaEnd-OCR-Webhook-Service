using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.App;

internal static partial class CliConfiguration
{
    private static bool IsFieldMissingOrTypeInvalid(JsonElement root, string fieldName, Arguments config, RuntimeOptions options)
    {
        if (!root.TryGetProperty(fieldName, out var property))
        {
            // Older configuration files retain case-insensitive window matching without a repair prompt.
            if (fieldName == nameof(Arguments.WindowTitleCaseSensitive)) return false;
            if (AllowEmptyWebhook(options) && string.IsNullOrWhiteSpace(config.WebhookUrl) && IsWebhookConfigField(fieldName))
            {
                return false;
            }

            return true;
        }

        return fieldName switch
        {
            nameof(Arguments.WindowTitle)
                or nameof(Arguments.SearchText)
                or nameof(Arguments.Language)
                or nameof(Arguments.WebhookUrl)
                or nameof(Arguments.WebhookBody)
                or nameof(Arguments.WebhookContentType)
                or nameof(Arguments.WebhookMode)
                    => property.ValueKind != JsonValueKind.String,

            nameof(Arguments.PartialMatch)
                or nameof(Arguments.WindowTitleCaseSensitive)
                or nameof(Arguments.CaseSensitive)
                    => property.ValueKind is not (JsonValueKind.True or JsonValueKind.False),

            nameof(Arguments.Retry)
                or nameof(Arguments.RetryInterval)
                or nameof(Arguments.RollingIntervalMs)
                or nameof(Arguments.WebhookTimeoutMs)
                or nameof(Arguments.WebhookPushCacheSeconds)
                    => property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out _),

            _ => true
        };
    }

    private static bool IsWebhookConfigField(string fieldName)
        => fieldName is nameof(Arguments.WebhookUrl)
            or nameof(Arguments.WebhookBody)
            or nameof(Arguments.WebhookContentType)
            or nameof(Arguments.WebhookTimeoutMs)
            or nameof(Arguments.WebhookMode)
            or nameof(Arguments.WebhookPushCacheSeconds);

    private static void ReadString(JsonElement root, string fieldName, Action<string> assign)
    {
        if (root.TryGetProperty(fieldName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            assign(property.GetString() ?? "");
        }
    }

    private static void ReadBoolean(JsonElement root, string fieldName, Action<bool> assign)
    {
        if (root.TryGetProperty(fieldName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            assign(property.GetBoolean());
        }
    }

    private static void ReadInteger(JsonElement root, string fieldName, Action<int> assign)
    {
        if (root.TryGetProperty(fieldName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out var value))
        {
            assign(value);
        }
    }

    private static bool RepairConfigFields(Arguments config, IReadOnlyList<string> fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            Logger.Info($"正在配置: {fieldName}");
            if (!RepairConfigField(config, fieldName))
            {
                Logger.Info("用户取消配置, 程序退出");
                return false;
            }
        }

        return true;
    }

    private static bool RepairConfigField(Arguments config, string fieldName)
    {
        switch (fieldName)
        {
            case nameof(Arguments.WindowTitle):
                var titleDefault = GetRequiredStringDefault(config.WindowTitle, Arguments.DefaultWindowTitle);
                if (!TryAskRequiredString($"请输入窗口标题 (默认: {titleDefault}, 输入 q 退出): ", titleDefault, out var windowTitle))
                {
                    return false;
                }

                config.WindowTitle = windowTitle;
                return true;

            case nameof(Arguments.WindowTitleCaseSensitive):
                config.WindowTitleCaseSensitive = AskYesNo("窗口标题是否区分大小写? (y/n, 默认: n 不区分): ", config.WindowTitleCaseSensitive);
                return true;

            case nameof(Arguments.SearchText):
                if (!TryAskRequiredString("请输入要查找的文本 (必填, 输入 q 退出): ", config.SearchText, out var searchText))
                {
                    return false;
                }

                config.SearchText = searchText;
                return true;

            case nameof(Arguments.PartialMatch):
                config.PartialMatch = AskYesNo("是否使用模糊匹配? (y/n, 默认: n 精确匹配): ", config.PartialMatch);
                return true;

            case nameof(Arguments.Retry):
                config.Retry = AskInteger("请输入重试次数 (1-10, 默认: 1): ", GetIntegerDefault(config.Retry, 1, 1, 10), 1, 10);
                return true;

            case nameof(Arguments.RetryInterval):
                config.RetryInterval = AskInteger("请输入重试间隔/毫秒 (100-60000, 默认: 1000): ", GetIntegerDefault(config.RetryInterval, 1000, 100, 60000), 100, 60000);
                return true;

            case nameof(Arguments.RollingIntervalMs):
                config.RollingIntervalMs = AskInteger("请输入滚动识别间隔/毫秒 (500-60000, 默认: 3000): ", GetIntegerDefault(config.RollingIntervalMs, 3000, 500, 60000), 500, 60000);
                return true;

            case nameof(Arguments.CaseSensitive):
                config.CaseSensitive = AskYesNo("日志区域锚点是否区分大小写? (y/n, 默认: n 不区分): ", config.CaseSensitive);
                return true;

            case nameof(Arguments.Language):
                var languageDefault = GetRequiredStringDefault(config.Language, "zh-Hans");
                if (!TryAskRequiredString($"请输入 OCR 语言标签 (默认: {languageDefault}): ", languageDefault, out var language))
                {
                    return false;
                }

                config.Language = language;
                return true;

            case nameof(Arguments.WebhookUrl):
                config.WebhookUrl = AskWebhookUrl(config.WebhookUrl);
                return true;

            case nameof(Arguments.WebhookBody):
                config.WebhookBody = AskWebhookBody(config.WebhookBody);
                return true;

            case nameof(Arguments.WebhookContentType):
                config.WebhookContentType = AskRequiredString($"请输入 Webhook Content-Type (默认: {GetRequiredStringDefault(config.WebhookContentType, "application/json")}): ", GetRequiredStringDefault(config.WebhookContentType, "application/json"));
                return true;

            case nameof(Arguments.WebhookTimeoutMs):
                config.WebhookTimeoutMs = AskInteger("请输入 Webhook 超时/毫秒 (1000-60000, 默认: 5000): ", GetIntegerDefault(config.WebhookTimeoutMs, 5000, 1000, 60000), 1000, 60000);
                return true;

            case nameof(Arguments.WebhookMode):
                config.WebhookMode = AskWebhookMode();
                return true;

            case nameof(Arguments.WebhookPushCacheSeconds):
                config.WebhookPushCacheSeconds = AskInteger("请输入 Webhook 推送缓存时间/秒 (0-86400, 0=不启用, 默认: 0): ", GetIntegerDefault(config.WebhookPushCacheSeconds, 0, 0, 86400), 0, 86400);
                return true;

            default:
                Logger.Warn($"未知配置项: {fieldName}");
                return true;
        }
    }

}
