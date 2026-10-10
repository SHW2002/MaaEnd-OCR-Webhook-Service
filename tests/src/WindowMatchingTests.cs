using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Gui;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class WindowMatchingTests
{
    public static void Run()
    {
        var marker = "window-match-" + Guid.NewGuid().ToString("N") + " ";
        using var target = new Form { Text = marker + "MaaEnd v2.13.0", StartPosition = FormStartPosition.Manual,
            Location = new Point(-1800, -1800) };
        target.Show(); target.Update();
        var exact = target.Text;
        var prefix = marker + "MaaEnd v";
        Program.Assert(WindowFinder.FindWindow(exact, false, true)?.Handle == target.Handle, "区分大小写时应匹配同大小写完整标题");
        Program.Assert(WindowFinder.FindWindow(exact.ToUpperInvariant(), false)?.Handle == target.Handle,
            "默认完整标题匹配必须忽略大小写");
        Program.Assert(WindowFinder.FindWindow(exact.ToUpperInvariant(), false, true) == null,
            "开启区分大小写后必须拒绝不同大小写完整标题");
        Program.Assert(WindowFinder.FindWindow(prefix.ToUpperInvariant(), false)?.Handle == target.Handle,
            "默认短标题包含匹配必须忽略大小写");
        Program.Assert(WindowFinder.FindWindow(prefix, false, true)?.Handle == target.Handle,
            "区分大小写应同时作用于短标题包含匹配");
        Program.Assert(WindowFinder.FindWindow(prefix.ToUpperInvariant(), true, true) == null,
            "模糊匹配也必须遵守大小写设置");

        var config = new Arguments { SearchText = "运行日志", WindowTitleCaseSensitive = true, CaseSensitive = false };
        Program.Assert(config.WindowTitle == "MaaEnd v", "默认窗口标题错误");
        var property = TypeDescriptor.GetProperties(new LocalizedSettings(config))[nameof(Arguments.WindowTitleCaseSensitive)]!;
        Program.Assert(property.DisplayName == "窗口标题区分大小写" && (bool)property.GetValue(config)!, "GUI 应提供窗口标题大小写配置");
        property.SetValue(config, false);
        Program.Assert(!config.WindowTitleCaseSensitive && !config.CaseSensitive, "窗口标题和锚点设置应独立");
        config.WindowTitleCaseSensitive = true;
        var snapshot = ConfigStore.RecognitionSnapshot(config);
        Program.Assert(snapshot.WindowTitleCaseSensitive && !snapshot.CaseSensitive, "数据集必须保留窗口标题大小写设置");
        var legacy = JsonNode.Parse(JsonSerializer.Serialize(config))!.AsObject();
        legacy.Remove(nameof(Arguments.WindowTitleCaseSensitive));
        Program.Assert(!JsonSerializer.Deserialize<Arguments>(legacy.ToJsonString())!.WindowTitleCaseSensitive,
            "旧配置缺少新字段时应保持忽略大小写");
        var path = Path.Combine(AppContext.BaseDirectory, "verification", "legacy-config-" + Guid.NewGuid().ToString("N") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, legacy.ToJsonString());
        var loaded = CliConfiguration.LoadOrCreateConfig(path, new RuntimeOptions { Debug = true });
        Program.Assert(loaded is { WindowTitleCaseSensitive: false }, "CLI 应兼容旧配置且不额外启动配置向导");
        target.Close();
        Console.WriteLine("PASS: window title exact/partial matching, case sensitivity, GUI setting, recording snapshot, legacy configuration");
    }
}
