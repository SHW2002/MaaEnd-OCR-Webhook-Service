using MaaEnd_Log_Retransmitter.Datasets;
using MaaEnd_Log_Retransmitter.Gui;

namespace MaaEnd_Log_Retransmitter.Tests;

internal static class RecordingWorkflowTests
{
    public static void Run(MainForm gui, DatasetStore source, string artifacts)
    {
        var output = Path.Combine(artifacts, "recording-workflow-" + Guid.NewGuid().ToString("N"));
        var page = Descendants(gui).OfType<RecordingPage>().Single();
        var pending = Copy(source, output);
        Prepare(gui, page, pending);
        Program.Assert(Button(page, "保存录制").Enabled && Button(page, "删除录制").Enabled,
            "停止后必须提供保存和删除");
        Program.Assert(!Button(page, "开始录制").Enabled && !Button(page, "停止").Enabled,
            "待保存录制不能被下一次录制覆盖");
        SaveScreenshot(gui, Path.Combine(artifacts, "recording-pending.png"));
        Button(page, "保存录制").PerformClick();
        Program.Assert(Directory.Exists(pending.SavedDirectory) && !Directory.Exists(pending.DirectoryPath), "保存必须移入正式会话目录");
        var saved = new DatasetStore(pending.SavedDirectory);
        Program.Assert(saved.LoadManifest().Frames.Count == source.LoadManifest().Frames.Count, "保存不能丢失录制帧");
        Program.Assert(Descendants(gui).OfType<EvaluationPage>().Single().Store?.DirectoryPath == saved.DirectoryPath,
            "只有保存后才进入该录制的评估页");
        ExpectFailure<InvalidOperationException>(pending.Delete);
        Program.Assert(Directory.Exists(pending.SavedDirectory), "不能通过待保存删除操作移除已保存数据集");

        var discarded = Copy(source, output);
        Prepare(gui, page, discarded);
        Button(page, "删除录制").PerformClick();
        Program.Assert(!Directory.Exists(discarded.DirectoryPath) && Directory.Exists(pending.SavedDirectory), "删除只能移除本次待保存会话");
        Program.Assert(Button(page, "开始录制").Enabled && !Button(page, "保存录制").Visible,
            "删除后应恢复录制入口");
        Program.Assert(Descendants(page).OfType<PictureBox>().Single().Image == null, "删除后应清空预览");

        var conflict = Copy(source, output);
        Directory.CreateDirectory(conflict.SavedDirectory);
        File.WriteAllText(Path.Combine(conflict.SavedDirectory, "existing.txt"), "retain");
        ExpectFailure<IOException>(() => conflict.Save());
        Program.Assert(Directory.Exists(conflict.DirectoryPath), "保存失败必须保留待处理数据");
        conflict.Delete();
        Program.Assert(File.Exists(Path.Combine(conflict.SavedDirectory, "existing.txt")), "不能覆盖或删除既有会话目录");

        var empty = PendingRecording.Create(output);
        gui.CompleteRecording(empty);
        Program.Assert(!Button(page, "保存录制").Enabled && Button(page, "删除录制").Enabled, "空录制只能删除");
        Button(page, "删除录制").PerformClick();
        VerifyClose(source, output, DialogResult.Cancel);
        VerifyClose(source, output, DialogResult.Yes);
        VerifyClose(source, output, DialogResult.No);
        Console.WriteLine("PASS: recording save/delete, saved session protection, failed save retention, empty recording, close decisions");
    }

    private static PendingRecording Copy(DatasetStore source, string output)
    {
        var pending = PendingRecording.Create(output);
        var destination = new DatasetStore(pending.DirectoryPath);
        var manifest = source.LoadManifest();
        foreach (var frame in manifest.Frames)
        {
            var target = destination.FramePath(frame);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source.FramePath(frame), target);
        }
        destination.SaveManifest(manifest); destination.SaveAnnotations(source.LoadAnnotations());
        return pending;
    }

    private static void Prepare(MainForm gui, RecordingPage page, PendingRecording pending)
    {
        page.PrepareRecording();
        var frame = new DatasetStore(pending.DirectoryPath).LoadManifest().Frames[0];
        page.ShowPreview(new(new DatasetStore(pending.DirectoryPath).FramePath(frame), pending.FrameCount,
            frame.OffsetMs, frame.Width, frame.Height));
        gui.CompleteRecording(pending); gui.Update();
    }

    private static void VerifyClose(DatasetStore source, string output, DialogResult decision)
    {
        using var gui = new MainForm(false);
        gui.Location = new Point(-1800, -1800); gui.Show();
        var pending = Copy(source, output);
        gui.CompleteRecording(pending);
        using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.OfType<RecordingDecisionForm>().FirstOrDefault();
            if (dialog == null) return;
            timer.Stop(); dialog.DialogResult = decision;
        };
        timer.Start(); gui.Close();
        if (decision == DialogResult.Cancel)
        {
            Program.Assert(!gui.IsDisposed && Directory.Exists(pending.DirectoryPath), "取消关闭必须保留录制和窗口");
            Button(Descendants(gui).OfType<RecordingPage>().Single(), "删除录制").PerformClick();
            gui.Close();
        }
        else
        {
            Program.Assert(gui.IsDisposed && !Directory.Exists(pending.DirectoryPath), "保存或删除成功后才能关闭");
            Program.Assert(Directory.Exists(pending.SavedDirectory) == (decision == DialogResult.Yes), "关闭决策必须正确保存或删除");
        }
    }

    private static void ExpectFailure<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("操作必须拒绝：" + typeof(T).Name);
    }
    private static Button Button(Control parent, string text) => Descendants(parent).OfType<Button>().Single(x => x.Text == text);
    private static IEnumerable<Control> Descendants(Control parent)
        => parent.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(Descendants(x)));
    private static void SaveScreenshot(Form form, string path)
    {
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(path);
    }
}
