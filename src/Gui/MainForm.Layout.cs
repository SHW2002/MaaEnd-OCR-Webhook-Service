namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class MainForm
{
    private readonly Panel _pages = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
    private ToolStripButton _runNavigation = null!;
    private ToolStripDropDownButton _developerNavigation = null!;

    private void InitializeLayout()
    {
        _runNavigation = UiLayout.ToolButton("运行日志", () => ShowPage(_run, "运行日志"));
        _runNavigation.Name = "RunNavigation";
        _developerNavigation = new ToolStripDropDownButton("开发选项") { Padding = new Padding(8, 3, 8, 3), Name = "DeveloperNavigation" };
        var record = new ToolStripMenuItem("录制数据集", null, (_, _) => ShowDevelopmentPage(false));
        var evaluate = new ToolStripMenuItem("评估数据集", null, (_, _) => ShowDevelopmentPage(true));
        _developerNavigation.DropDownItems.AddRange([record, evaluate]);
        var settings = UiLayout.ToolButton("配置", EditConfiguration);
        settings.Alignment = ToolStripItemAlignment.Right;
        var navigation = UiLayout.Toolbar(_runNavigation, _developerNavigation, new ToolStripSeparator(), settings);
        navigation.Font = Theme.Font;
        var status = new StatusStrip { BackColor = Theme.Surface, ForeColor = Theme.Muted,
            SizingGrip = false, Padding = new Padding(10, 2, 10, 2) };
        status.Items.Add(_status);
        foreach (var page in new Control[] { _run, _record, _evaluate })
        { page.Dock = DockStyle.Fill; page.Visible = false; _pages.Controls.Add(page); }
        Controls.Add(_pages); Controls.Add(status); Controls.Add(navigation);
        ShowPage(_run, "运行日志");
    }

    internal void ShowDevelopmentPage(bool evaluate)
        => ShowPage(evaluate ? _evaluate : _record, evaluate ? "评估数据集" : "录制数据集");

    private void ShowPage(Control selected, string title)
    {
        foreach (Control page in _pages.Controls) page.Visible = ReferenceEquals(page, selected);
        selected.BringToFront();
        _runNavigation.Checked = ReferenceEquals(selected, _run);
        _developerNavigation.Text = ReferenceEquals(selected, _run) ? "开发选项" : "开发选项 / " + title;
        Text = "MaaEnd 日志转发器 · " + title;
    }

}
