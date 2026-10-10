using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.App;

internal static partial class CliConfiguration
{
    private const string WebhookBodyTemplateFileName = "webhook-body-template.json";
    private const string WebhookBodyInstructionStart = "----- WEBHOOK BODY INSTRUCTIONS START -----";
    private const string WebhookBodyInstructionEnd = "----- WEBHOOK BODY INSTRUCTIONS END -----";
    private static readonly string[] ConfigFieldOrder =
    [
        nameof(Arguments.WindowTitle),
        nameof(Arguments.WindowTitleCaseSensitive),
        nameof(Arguments.SearchText),
        nameof(Arguments.PartialMatch),
        nameof(Arguments.Retry),
        nameof(Arguments.RetryInterval),
        nameof(Arguments.RollingIntervalMs),
        nameof(Arguments.CaseSensitive),
        nameof(Arguments.Language),
        nameof(Arguments.WebhookUrl),
        nameof(Arguments.WebhookBody),
        nameof(Arguments.WebhookContentType),
        nameof(Arguments.WebhookTimeoutMs),
        nameof(Arguments.WebhookMode),
        nameof(Arguments.WebhookPushCacheSeconds)
    ];
    public static Arguments? LoadOrCreateConfig(string configPath, RuntimeOptions options)
    {
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath, Encoding.UTF8);
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    Logger.Warn("配置文件格式无效, 进入交互式配置...");
                }
                else
                {
                    var config = ReadConfigFromJson(document.RootElement);
                    var invalidFields = GetConfigFieldsNeedingRepair(document.RootElement, config, options);
                    if (invalidFields.Count > 0)
                    {
                        Logger.Warn("配置文件存在缺失项、空值或非法值, 将依次启动对应配置向导...");
                        foreach (var fieldName in invalidFields)
                        {
                            Logger.Warn($"配置错误: {Arguments.GetValidationMessage(fieldName)}");
                        }

                        if (!RepairConfigFields(config, invalidFields))
                        {
                            return null;
                        }

                        if (!config.TryValidate(AllowEmptyWebhook(options), out var repairedValidationErrors))
                        {
                            foreach (var error in repairedValidationErrors)
                            {
                                Logger.Error($"配置错误: {error}");
                            }

                            return null;
                        }

                        SaveConfig(configPath, config);
                    }

                    if (!config.TryValidate(AllowEmptyWebhook(options), out var validationErrors))
                    {
                        foreach (var error in validationErrors)
                        {
                            Logger.Error($"配置错误: {error}");
                        }

                        return null;
                    }

                    Logger.Info($"已加载配置文件: {configPath}");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"读取配置文件失败: {ex.Message}, 进入交互式配置...");
            }
        }
        else
        {
            Logger.Info("未检测到配置文件, 进入交互式配置...");
        }

        var newConfig = InteractivePrompt();
        if (newConfig == null)
        {
            return null;
        }

        SaveConfig(configPath, newConfig);
        return newConfig;
    }

    private static void SaveConfig(string configPath, Arguments config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json, Encoding.UTF8);
            Logger.Info($"配置已保存到: {configPath}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"保存配置文件失败: {ex.Message}");
        }
    }

    private static Arguments ReadConfigFromJson(JsonElement root)
    {
        var config = new Arguments();

        ReadString(root, nameof(Arguments.WindowTitle), value => config.WindowTitle = value);
        ReadBoolean(root, nameof(Arguments.WindowTitleCaseSensitive), value => config.WindowTitleCaseSensitive = value);
        ReadString(root, nameof(Arguments.SearchText), value => config.SearchText = value);
        ReadBoolean(root, nameof(Arguments.PartialMatch), value => config.PartialMatch = value);
        ReadInteger(root, nameof(Arguments.Retry), value => config.Retry = value);
        ReadInteger(root, nameof(Arguments.RetryInterval), value => config.RetryInterval = value);
        ReadInteger(root, nameof(Arguments.RollingIntervalMs), value => config.RollingIntervalMs = value);
        ReadBoolean(root, nameof(Arguments.CaseSensitive), value => config.CaseSensitive = value);
        ReadString(root, nameof(Arguments.Language), value => config.Language = value);
        ReadString(root, nameof(Arguments.WebhookUrl), value => config.WebhookUrl = value);
        ReadString(root, nameof(Arguments.WebhookBody), value => config.WebhookBody = value);
        ReadString(root, nameof(Arguments.WebhookContentType), value => config.WebhookContentType = value);
        ReadInteger(root, nameof(Arguments.WebhookTimeoutMs), value => config.WebhookTimeoutMs = value);
        ReadString(root, nameof(Arguments.WebhookMode), value => config.WebhookMode = value);
        ReadInteger(root, nameof(Arguments.WebhookPushCacheSeconds), value => config.WebhookPushCacheSeconds = value);

        return config;
    }

    private static IReadOnlyList<string> GetConfigFieldsNeedingRepair(JsonElement root, Arguments config, RuntimeOptions options)
    {
        var invalidFields = new HashSet<string>(StringComparer.Ordinal);

        foreach (var fieldName in ConfigFieldOrder)
        {
            if (IsFieldMissingOrTypeInvalid(root, fieldName, config, options))
            {
                invalidFields.Add(fieldName);
            }
        }

        foreach (var fieldName in config.GetInvalidFields(AllowEmptyWebhook(options)))
        {
            invalidFields.Add(fieldName);
        }

        return ConfigFieldOrder.Where(invalidFields.Contains).ToList();
    }

    private static bool IsWebhookEnabled(Arguments args, RuntimeOptions options)
        => !(options.Debug && string.IsNullOrWhiteSpace(args.WebhookUrl)) && args.HasWebhookUrl;

    private static bool AllowEmptyWebhook(RuntimeOptions options)
        => options.Debug;

}
