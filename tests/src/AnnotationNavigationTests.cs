using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Gui;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class AnnotationNavigationTests
{
    public static void Run(string artifacts)
    {
        var store = CreateDataset(artifacts);
        var original = File.ReadAllText(store.AnnotationsPath);
        using var owner = new Form { Location = new Point(-1800, -1800), StartPosition = FormStartPosition.Manual, Size = new Size(1100, 740) };
        var page = new EvaluationPage { Dock = DockStyle.Fill };
        owner.Controls.Add(page); owner.Show(); page.LoadDataset(store.ManifestPath);
        var browser = Descendants(page).OfType<DatasetBrowser>().Single();
        var groups = Descendants(browser).OfType<ComboBox>().Single();
        var slider = Descendants(browser).OfType<TrackBar>().Single();
        var grid = Descendants(browser).OfType<DataGridView>().Single();
        Program.Assert(groups.Items.Count == 5 && slider.Value == 2, "相同文字的连续帧必须归为同组，载入定位文字组");
        Program.Assert(browser.AnnotationTextCount == 3, "总览须保留新时间的相同正文，并排除不存在的帧标注");
        var firstText = (AnnotationGroup)groups.SelectedItem!;
        Program.Assert(firstText.StartPosition == 2 && firstText.EndPosition == 3, "文字相同而确认状态不同的帧也应能一起跳过");
        Button(browser, "下一组").PerformClick();
        Program.Assert(slider.Value == 4 && grid.Rows.Count == 3, "下一组须跳到包含多条真值的下一次文字变化");
        Button(browser, "上一组").PerformClick();
        Program.Assert(slider.Value == 2, "上一组必须定位该组首帧");
        slider.Value = 3;
        Program.Assert(groups.SelectedIndex == 1, "逐帧定位须同步标注组选择");
        Button(browser, "下一组").PerformClick();
        Program.Assert(slider.Value == 4, "在相同文字组的中间也必须跳过重复帧");
        groups.SelectedIndex = 3;
        Program.Assert(slider.Value == 5 && grid.Rows[0].Cells[1].Value?.ToString() == "连接成功\n设备就绪",
            "下拉选择须定位正确帧并保留完整多行文本");
        VerifyOverview(browser);
        Program.Assert(slider.Value == 6 && !Button(browser, "下一组").Enabled, "总览选择必须定位对应帧并正确处理末组");
        Program.Assert(File.ReadAllText(store.AnnotationsPath) == original, "浏览全部文本不得修改真值和确认状态");
        slider.Value = 0;
        Program.Assert(!Button(browser, "上一组").Enabled, "首组不能跳到范围外");
        slider.Value = 3;
        grid.Rows[0].Cells[1].Value = "修改后的真值"; browser.SaveCurrent();
        Program.Assert(groups.Items.Count == 6 && groups.SelectedIndex == 2 && browser.AnnotationTextCount == 4,
            "编辑保存须重新计算文字变化和文本总览");
        Program.Assert(!store.LoadAnnotations().Frames[27].Reviewed, "浏览和编辑不应自动确认真值");
        owner.Close();
        Console.WriteLine("PASS: annotation group navigation, repeated-frame skipping, full-text overview, locate frame, review preservation");
    }

    private static void VerifyOverview(DatasetBrowser browser)
    {
        Exception? failure = null;
        using var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += (_, _) =>
        {
            var overview = Application.OpenForms.OfType<AnnotationOverviewForm>().SingleOrDefault();
            if (overview == null) return;
            timer.Stop();
            try
            {
                var grid = Descendants(overview).OfType<DataGridView>().Single();
                Program.Assert(grid.ReadOnly && grid.Rows.Count == 3, "全部标注必须显示所有不同文本，包含未确认标注");
                Program.Assert(grid.Rows[1].Cells[2].Value?.ToString() == "连接成功\n设备就绪", "总览必须显示完整多行正文");
                grid.CurrentCell = grid.Rows[2].Cells[2];
                Button(overview, "定位帧").PerformClick();
            }
            catch (Exception ex) { failure = ex; overview.DialogResult = DialogResult.Cancel; }
        };
        timer.Start(); Button(browser, "全部标注").PerformClick();
        if (failure != null) throw failure;
    }

    private static DatasetStore CreateDataset(string artifacts)
    {
        var store = new DatasetStore(Path.Combine(artifacts, "annotation-navigation-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(store.DirectoryPath);
        var manifest = new DatasetManifest();
        var truth = new DatasetAnnotations();
        var indexes = new[] { 3, 9, 15, 27, 40, 41, 89 };
        var first = new TruthLine("12:00:01", "任务开始");
        var second = new TruthLine("12:00:02", "连接成功\n设备就绪");
        List<TruthLine>[] lines = [[], [], [first], [first], [first, second], [second], [new("12:00:02", "任务开始")]];
        using var image = new Bitmap(80, 60);
        for (var i = 0; i < indexes.Length; i++)
        {
            var frame = new DatasetFrame(indexes[i], $"{i}.png", i * 100, DateTimeOffset.UtcNow, 80, 60, 96, "fixture", 0, "");
            image.Save(store.FramePath(frame)); manifest.Frames.Add(frame);
            truth.Frames[indexes[i]] = new() { Reviewed = i == 2, Lines = lines[i] };
        }
        truth.Frames[999] = new() { Lines = [new("", "不属于此录制的标注")] };
        store.SaveManifest(manifest); store.SaveAnnotations(truth);
        return store;
    }

    private static Button Button(Control parent, string text) => Descendants(parent).OfType<Button>().Single(x => x.Text == text);
    private static IEnumerable<Control> Descendants(Control parent)
        => parent.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(Descendants(x)));
}
