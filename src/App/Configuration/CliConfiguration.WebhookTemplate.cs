using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.App;

internal static partial class CliConfiguration
{
    private static string PrepareWebhookBodyTemplateFile(string defaultValue)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WebhookBodyTemplateFileName);

        try
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, BuildWebhookBodyTemplateFileContent(defaultValue), Encoding.UTF8);
                return path;
            }

            var existing = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(existing))
            {
                File.WriteAllText(path, BuildWebhookBodyTemplateFileContent(defaultValue), Encoding.UTF8);
            }
            else if (!existing.Contains(WebhookBodyInstructionStart, StringComparison.Ordinal))
            {
                File.WriteAllText(path, BuildWebhookBodyTemplateFileContent(existing), Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"准备 Webhook Body 模板文件失败: {ex.Message}");
        }

        return path;
    }

    private static string BuildWebhookBodyTemplateFileContent(string body)
    {
        var instructions = string.Join(Environment.NewLine, [
            WebhookBodyInstructionStart,
            "# 请在下方编辑 Webhook Body 模板。",
            "# 必须保留 __CONTENT__ 占位符，程序会用 OCR 内容替换它。",
            "# 可选使用 __TIME__ 占位符，程序会用 OCR 时间替换它。",
            "# 保存并关闭记事本后，CLI 会继续。",
            "# Edit the Webhook Body template below.",
            "# Keep the __CONTENT__ placeholder; the app will replace it with OCR content.",
            "# Optionally use the __TIME__ placeholder; the app will replace it with OCR time.",
            "# Save and close Notepad, then the CLI will continue.",
            WebhookBodyInstructionEnd,
            ""
        ]);

        return instructions + body;
    }

    private static bool OpenWebhookBodyTemplateEditor(string path)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "notepad.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.ArgumentList.Add(path);

            if (!process.Start())
            {
                return false;
            }

            process.WaitForExit();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"打开记事本失败: {ex.Message}");
            return false;
        }
    }

    private static string ReadWebhookBodyTemplateFile(string path)
    {
        try
        {
            var content = File.ReadAllText(path, Encoding.UTF8);
            return StripWebhookBodyTemplateInstructions(content);
        }
        catch (Exception ex)
        {
            Logger.Warn($"读取 Webhook Body 模板文件失败: {ex.Message}");
            return "";
        }
    }

    private static string StripWebhookBodyTemplateInstructions(string content)
    {
        var start = content.IndexOf(WebhookBodyInstructionStart, StringComparison.Ordinal);
        if (start < 0)
        {
            return content;
        }

        var end = content.IndexOf(WebhookBodyInstructionEnd, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return content;
        }

        end += WebhookBodyInstructionEnd.Length;
        while (end < content.Length && (content[end] == '\r' || content[end] == '\n'))
        {
            end++;
        }

        return content.Remove(start, end - start);
    }

    private static string AskRequiredString(string prompt, string defaultValue)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim() ?? "";
            var value = string.IsNullOrWhiteSpace(input) ? defaultValue : input;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Logger.Warn("输入无效, 该配置不能为空");
        }
    }

    private static string AskWebhookMode()
    {
        while (true)
        {
            Console.Write("请选择 Webhook 推送方式 (1=仅实时, 2=仅总结, 3=全部, 默认: 1): ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(input)) return "Realtime";

            var mode = input switch
            {
                "1" => "Realtime",
                "2" => "Summary",
                "3" => "All",
                _ => input
            };

            if (Arguments.TryNormalizeWebhookMode(mode, out var normalized))
            {
                return normalized;
            }

            Logger.Warn("输入无效, 请输入 1、2、3，或 Realtime/Summary/All");
        }
    }

    private static void PrintSummary(Arguments config)
    {
        Console.WriteLine();
        Console.WriteLine("══════════ 配置摘要 ══════════");
        Console.WriteLine($"  窗口标题:     {config.WindowTitle}");
        Console.WriteLine($"  搜索文本:     {config.SearchText}");
        Console.WriteLine($"  匹配模式:     {(config.PartialMatch ? "模糊匹配" : "精确匹配")}");
        Console.WriteLine($"  区分大小写:   {(config.CaseSensitive ? "是" : "否")}");
        Console.WriteLine($"  OCR 语言:     {config.Language}");
        Console.WriteLine($"  重试次数:     {config.Retry}");
        Console.WriteLine($"  重试间隔:     {config.RetryInterval}ms");
        Console.WriteLine($"  滚动间隔:     {config.RollingIntervalMs}ms");
        Console.WriteLine("  Webhook:      启用");
        Console.WriteLine($"  Webhook URL:  {config.WebhookUrl}");
        Console.WriteLine($"  Content-Type: {config.WebhookContentType}");
        Console.WriteLine($"  推送方式:     {config.WebhookModeDisplay}");
        Console.WriteLine($"  推送超时:     {config.WebhookTimeoutMs}ms");
        Console.WriteLine($"  推送缓存时间: {config.WebhookPushCacheSeconds}s");
        Console.WriteLine($"  Body 模板:    {config.WebhookBody}");
        Console.WriteLine("═══════════════════════════════");
        Console.WriteLine();
    }}
