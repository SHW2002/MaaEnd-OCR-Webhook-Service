using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Gui;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class GuiSmoke
{
    public static void Run(DatasetStore store, string directory)
    {
        DatasetBrowserTests.Run(directory);
        AnnotationNavigationTests.Run(directory);
        RecordingSessionTests.Run(directory);
        using var gui = new MainForm(false);
        gui.Location = new Point(-1800, -1800); gui.Show(); gui.Update();
        Save(gui, Path.Combine(directory, "gui.png"));
        gui.ShowDevelopmentPage(false);
        var recording = Descendants(gui).OfType<RecordingPage>().Single();
        var manifest = store.LoadManifest();
        var first = manifest.Frames[0];
        recording.ShowPreview(new(store.FramePath(first), manifest.Frames.Count, first.OffsetMs, first.Width, first.Height));
        gui.Update(); Save(gui, Path.Combine(directory, "recording.png"));
        RecordingWorkflowTests.Run(gui, store, directory);
        gui.ShowDevelopmentPage(true);
        var evaluation = Descendants(gui).OfType<EvaluationPage>().Single();
        evaluation.LoadDataset(store.ManifestPath);
        gui.Update(); Save(gui, Path.Combine(directory, "evaluation.png"));
        var browser = Descendants(evaluation).OfType<DatasetBrowser>().Single();
        Program.Assert(Descendants(browser).OfType<PictureBox>().Single().Height > 100, "高 DPI 下录制画面不能被其他控件挤没");
        gui.Update(); Save(gui, Path.Combine(directory, "annotations.png"));
        Program.Assert(Descendants(browser).OfType<DataGridView>().Single().Height > 60, "高 DPI 下必须保留可编辑的标注表格");
        evaluation.SaveAnnotations();
        Program.Assert(store.LoadAnnotations().EventsReviewed, "浏览器不能丢失事件真值");
        using (var configuration = new ConfigurationForm(App.ConfigStore.Load()))
        { configuration.Show(gui); configuration.Update(); Save(configuration, Path.Combine(directory, "configuration.png")); configuration.Close(); }
        using (var events = new EventAnnotationsForm(store))
        { events.Show(gui); events.Update(); Save(events, Path.Combine(directory, "events.png")); events.Close(); }
        gui.Size = gui.MinimumSize;
        gui.Update(); Save(gui, Path.Combine(directory, "compact.png"));
        Program.Assert(Descendants(browser).OfType<DataGridView>().Single().Height > 140,
            "紧凑窗口下必须保留多条标注的可见空间");
        foreach (var button in Descendants(browser).OfType<Button>())
            Program.Assert(button.Parent!.ClientRectangle.Contains(button.Bounds), "紧凑窗口下标注按钮必须完整显示");
        foreach (var control in Descendants(evaluation).Where(x => x is Button or CheckBox or NumericUpDown && !browser.Contains(x)))
            Program.Assert(evaluation.RectangleToScreen(evaluation.ClientRectangle).Contains(control.RectangleToScreen(control.ClientRectangle)),
                $"紧凑窗口下评估操作必须完整显示：{control.Text}");
        gui.Close();
    }
    private static void Save(Form form, string path)
    {
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(path);
    }
    private static IEnumerable<Control> Descendants(Control parent)
        => parent.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(Descendants(x)));
}
