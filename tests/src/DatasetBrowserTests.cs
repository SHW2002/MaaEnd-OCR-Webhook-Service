using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Gui;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class DatasetBrowserTests
{
    public static void Run(string artifacts)
    {
        var store = CreateDataset(artifacts);
        using var page = new EvaluationPage();
        Program.Assert(page.ProductionTiming, "GUI 必须默认按正式运行频率采样");
        Program.Assert(page.WorkerCount == ReplaySegments.DefaultWorkers, "GUI 必须提供默认计算线程数");
        var workers = Descendants(page).OfType<NumericUpDown>().Single(x => x.AccessibleName == "计算线程");
        workers.Value = 1;
        Program.Assert(page.WorkerCount == 1, "GUI 必须支持单线程连续回放");
        workers.Value = 4;
        Program.Assert(page.WorkerCount == 4, "GUI 必须支持手动指定多线程数");
        page.SetBusy(true);
        Program.Assert(!workers.Enabled, "评估时不能改变线程数");
        page.SetBusy(false);
        Program.Assert(workers.Enabled, "停止评估后必须恢复线程数选项");
        var timing = Descendants(page).OfType<CheckBox>().Single(x => x.Text == "按正式频率采样");
        timing.Checked = false;
        Program.Assert(!page.ProductionTiming, "取消采样才能切换为逐帧模式");
        timing.Checked = true;
        page.LoadDataset(store.ManifestPath);
        var browser = Descendants(page).OfType<DatasetBrowser>().Single();
        var grid = Descendants(browser).OfType<DataGridView>().Single();
        var slider = Descendants(browser).OfType<TrackBar>().Single();
        Program.Assert(slider.Value == 1 && Content(grid) == "原始真值", "载入必须定位首个有正文标注的帧");
        Program.Assert(browser.AnnotationSummary.Contains("1 帧有文字") && browser.AnnotationSummary.Contains("2 帧已确认"),
            "必须显示已读取的正文和确认帧数量");
        var relative = DatasetPaths.Display(store.DirectoryPath);
        Program.Assert(!Path.IsPathRooted(relative), "同盘数据集必须显示相对路径");
        Program.Assert(Descendants(page).OfType<TextBox>().Any(x => x.Text == relative), "数据集路径框必须显示相对会话目录");
        Program.Assert(DatasetPaths.Resolve(relative) == store.DirectoryPath, "相对路径必须按程序目录解析");
        page.LoadDataset(relative);
        Program.Assert(page.Store?.DirectoryPath == store.DirectoryPath, "显示的相对路径必须能重新载入数据集");

        var latest = store.LoadAnnotations();
        latest.Frames[11].Lines = [new("12:00:01", "磁盘更新真值")];
        store.SaveAnnotations(latest);
        Button(browser, "重载标注").PerformClick();
        Program.Assert(Content(grid) == "磁盘更新真值", "重载必须刷新当前帧的正文表格");
        grid.Rows[0].Cells[1].Value = "GUI 修改真值";
        latest = store.LoadAnnotations();
        latest.Frames[20] = new() { Reviewed = true, Lines = [new("12:00:02", "外部新增真值")] };
        latest.EventsReviewed = true;
        latest.Events = [new("12:00:02", "外部事件真值", 200)];
        store.SaveAnnotations(latest);
        browser.SaveCurrent();
        var saved = store.LoadAnnotations();
        Program.Assert(saved.Frames[11].Lines[0].Content == "GUI 修改真值", "GUI 编辑必须写入对应帧编号");
        Program.Assert(saved.Frames[20].Lines[0].Content == "外部新增真值", "保存当前帧不能覆盖磁盘上其它帧的标注");
        Program.Assert(saved.EventsReviewed && saved.Events.Single().Content == "外部事件真值", "保存当前帧不能覆盖磁盘事件真值");
        Program.Assert(browser.AnnotationSummary.Contains("2 帧有文字"), "保存后必须更新标注统计");
        Program.Assert(grid.Columns.Cast<DataGridViewColumn>().All(x => x.SortMode == DataGridViewColumnSortMode.NotSortable),
            "表头排序不能改变日志真值顺序");

        File.Delete(store.AnnotationsPath);
        browser.ReloadAnnotations();
        Program.Assert(browser.AnnotationSummary == "未找到 annotations.json" && grid.Rows.Count == 1,
            "缺少标注文件时必须明确提示并清除旧正文");
        Console.WriteLine("PASS: annotation positioning, reload, save preservation, missing file status, relative dataset paths");
    }

    private static DatasetStore CreateDataset(string artifacts)
    {
        var store = new DatasetStore(Path.Combine(artifacts, "browser-regression-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(store.DirectoryPath);
        var manifest = new DatasetManifest();
        using var image = new Bitmap(80, 60);
        var indexes = new[] { 4, 11, 20 };
        for (var i = 0; i < indexes.Length; i++)
        {
            var frame = new DatasetFrame(indexes[i], $"{i}.png", i * 100, DateTimeOffset.UtcNow, 80, 60, 96, "fixture", 0, "");
            image.Save(store.FramePath(frame)); manifest.Frames.Add(frame);
        }
        store.SaveManifest(manifest);
        store.SaveAnnotations(new()
        {
            Frames = new()
            {
                [4] = new() { Reviewed = true },
                [11] = new() { Reviewed = true, Lines = [new("12:00:01", "原始真值")] }
            }
        });
        return store;
    }

    private static string? Content(DataGridView grid) => grid.Rows[0].Cells[1].Value?.ToString();
    private static Button Button(Control parent, string text) => Descendants(parent).OfType<Button>().Single(x => x.Text == text);
    private static IEnumerable<Control> Descendants(Control parent)
        => parent.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(Descendants(x)));
}
