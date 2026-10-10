namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class RunPage : UserControl
{
    private readonly TextBox _log = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
        ScrollBars = ScrollBars.Both, WordWrap = false, BorderStyle = BorderStyle.None,
        BackColor = Theme.Surface, ForeColor = Theme.Text };
    private readonly CheckBox _debug = UiLayout.Check("调试");
    private readonly Button _start;
    private readonly Button _stop;
    public bool DebugEnabled => _debug.Checked;
    public event Action? StartRequested;
    public event Action? StopRequested;

    public RunPage(bool debug)
    {
        AutoScaleMode = AutoScaleMode.None;
        _debug.Checked = debug;
        _start = Ui.Button("开始识别", () => StartRequested?.Invoke());
        Theme.Primary(_start);
        _stop = Ui.Button("停止", () => StopRequested?.Invoke());
        _stop.Enabled = false;
        var clear = Ui.Button("清空日志", () => _log.Clear());
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
            Padding = new Padding(0, 0, 0, 8), Margin = Padding.Empty };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.Controls.Add(Ui.Row(_start, _stop, _debug), 0, 0); toolbar.Controls.Add(clear, 1, 0);
        Controls.Add(UiLayout.Surface(_log, 10)); Controls.Add(toolbar);
    }
    public void SetBusy(bool busy) { _start.Enabled = !busy; _stop.Enabled = busy; _debug.Enabled = !busy; }
    public void AppendLog(string message)
    {
        if (_log.TextLength > 120000) _log.Text = _log.Text[^60000..];
        _log.AppendText(message + Environment.NewLine);
    }
}
