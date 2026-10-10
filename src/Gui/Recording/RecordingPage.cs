using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class RecordingPage : UserControl
{
    private readonly TextBox _output = Ui.Text("records");
    private readonly TextBox _scenario = Ui.Text("默认窗口 / 正常日志");
    private readonly NumericUpDown _interval = new() { Minimum = 33, Maximum = 60000, Value = 100,
        Width = 84, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 3, 12, 3) };
    private readonly Button _start;
    private readonly Button _stop;
    private readonly Button _save;
    private readonly Button _delete;
    private readonly Button _open;
    private readonly TableLayoutPanel _folder;
    private readonly PictureBox _preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Theme.Canvas };
    private readonly Label _placeholder = new() { Dock = DockStyle.Fill, Text = "开始录制后显示目标窗口画面",
        TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted, BackColor = Theme.Canvas };
    private readonly Label _info = Ui.Label("目标窗口录制预览");
    private string? _lastDirectory;
    private bool _pending;
    private bool _busy;
    private bool _acceptPreview = true;
    private int _frameCount;
    public string OutputDirectory => DatasetPaths.Resolve(_output.Text);
    public string Scenario => _scenario.Text;
    public int IntervalMs => (int)_interval.Value;
    public event Action? StartRequested;
    public event Action? StopRequested;
    public event Action? SettingsRequested;
    public event Action? SaveRequested;
    public event Action? DeleteRequested;

    public RecordingPage()
    {
        AutoScaleMode = AutoScaleMode.None;
        _start = Ui.Button("开始录制", () => StartRequested?.Invoke()); Theme.Primary(_start);
        _stop = Ui.Button("停止", () => StopRequested?.Invoke()); _stop.Enabled = false;
        _save = Ui.Button("保存录制", () => SaveRequested?.Invoke()); Theme.Primary(_save);
        _delete = Ui.Button("删除录制", () => DeleteRequested?.Invoke());
        _save.Visible = false; _delete.Visible = false;
        _open = Ui.Button("打开录制目录", () => { if (_lastDirectory != null) Ui.OpenFolder(_lastDirectory); });
        _open.Enabled = false;
        _folder = UiLayout.Field("保存目录", _output, Ui.Button("浏览", () =>
        {
            var value = Ui.BrowseFolder(this, DatasetPaths.BrowseDirectory(_output.Text));
            if (value != null) _output.Text = DatasetPaths.Display(value);
        }));
        var options = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, Padding = new Padding(0, 2, 0, 8) };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        options.Controls.Add(Ui.Label("采集间隔（毫秒）"), 0, 0); options.Controls.Add(_interval, 1, 0);
        options.Controls.Add(Ui.Label("场景"), 2, 0); _scenario.Dock = DockStyle.Fill; options.Controls.Add(_scenario, 3, 0);
        var actions = Ui.Row(_start, _stop, _save, _delete, Ui.Button("目标窗口", () => SettingsRequested?.Invoke()), _open);
        actions.Padding = new Padding(0, 0, 0, 8);
        var preview = new Panel { Dock = DockStyle.Fill };
        preview.Controls.Add(_preview); preview.Controls.Add(_placeholder);
        _info.Dock = DockStyle.Bottom; _info.Padding = new Padding(2, 6, 2, 0);
        Controls.Add(UiLayout.Surface(preview, 1)); Controls.Add(_info); Controls.Add(UiLayout.Stack(_folder, options, actions));
        Disposed += (_, _) => _preview.Image?.Dispose();
    }
    public void SetBusy(bool busy)
    { _busy = busy; UpdateActions(); }
    public void PrepareRecording()
    {
        _lastDirectory = null; _pending = false; _frameCount = 0; _acceptPreview = true;
        ClearPreview(); _info.Text = "正在采集目标窗口…"; UpdateActions();
    }
    public void SetPending(int frameCount)
    {
        _pending = true; _frameCount = frameCount; _acceptPreview = false;
        _info.Text = $"录制已结束  ·  {frameCount} 帧  ·  请选择保存或删除";
        UpdateActions();
    }
    public void SetSaved(string directory)
    {
        _pending = false; _lastDirectory = directory; _acceptPreview = false;
        _info.Text = $"录制已保存  ·  {_frameCount} 帧"; UpdateActions();
    }
    public void SetDeleted()
    {
        _pending = false; _lastDirectory = null; _frameCount = 0; _acceptPreview = false;
        ClearPreview(); _info.Text = "已删除本次录制，可重新开始"; UpdateActions();
    }
    private void UpdateActions()
    {
        _start.Enabled = !_busy && !_pending; _stop.Enabled = _busy;
        _save.Visible = _pending; _delete.Visible = _pending;
        _save.Enabled = !_busy && _pending && _frameCount > 0; _delete.Enabled = !_busy && _pending;
        _open.Enabled = !_busy && _lastDirectory != null;
        _folder.Enabled = !_busy && !_pending; _interval.Enabled = !_busy && !_pending; _scenario.Enabled = !_busy && !_pending;
    }
    private void ClearPreview()
    { var old = _preview.Image; _preview.Image = null; old?.Dispose(); _placeholder.Visible = true; }
    public void ShowPreview(RecordingPreview frame)
    {
        if (IsDisposed || !_acceptPreview) return;
        try
        {
            using var source = new Bitmap(frame.Path);
            var image = new Bitmap(source); var old = _preview.Image; _preview.Image = image; old?.Dispose();
            _placeholder.Visible = false;
            _info.Text = $"{frame.Count} 帧  ·  {frame.OffsetMs / 1000:F1} 秒  ·  {frame.Width} × {frame.Height}";
        }
        catch (IOException) { }
        catch (ArgumentException) { }
    }
}
