using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class DatasetBrowser : UserControl
{
    private readonly PictureBox _image = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Theme.Canvas };
    private readonly TrackBar _frame = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 0, TickStyle = TickStyle.None, AutoSize = false };
    private readonly ToolStripLabel _info = new("未选择数据集") { ForeColor = Theme.Muted };
    private readonly DataGridView _lines = new() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = true, AllowUserToDeleteRows = true, RowHeadersVisible = false };
    private readonly CheckBox _reviewed = UiLayout.Check("真值已确认");
    private readonly CheckBox _anchor = UiLayout.Check("锚点可见");
    private readonly TextBox _scenario = Ui.Text("", 200);
    private readonly System.Windows.Forms.Timer _timer = new();
    private ToolStripButton _play = null!;
    private DatasetStore? _store;
    private DatasetManifest? _manifest;
    private DatasetAnnotations _annotations = new();
    private int _index = -1;
    private bool _loading;
    private bool _dirty;
    public event Action? AnnotationsChanged;

    public DatasetBrowser()
    {
        AutoScaleMode = AutoScaleMode.None;
        InitializeLayout();
        AnnotationsChanged += RebuildAnnotationNavigation;
        _frame.ValueChanged += (_, _) => { if (!_loading) SelectFrame(_frame.Value); };
        _timer.Tick += (_, _) => AdvancePlayback();
        _lines.CellValueChanged += (_, _) => { if (!_loading) _dirty = true; };
        _lines.UserDeletedRow += (_, _) => { if (!_loading) _dirty = true; };
        _reviewed.CheckedChanged += (_, _) => { if (!_loading) _dirty = true; };
        _anchor.CheckedChanged += (_, _) => { if (!_loading) _dirty = true; };
        _scenario.TextChanged += (_, _) => { if (!_loading) _dirty = true; };
        _lines.Resize += (_, _) => _lines.AutoResizeRows();
        Disposed += (_, _) => { _timer.Dispose(); _image.Image?.Dispose(); };
    }

    public void LoadDataset(DatasetStore store, DatasetManifest manifest)
    {
        var annotations = store.LoadAnnotations();
        Pause(); _store = store; _manifest = manifest; _annotations = annotations; _index = -1;
        var first = manifest.Frames.FindIndex(frame => annotations.Frames.GetValueOrDefault(frame.Index)?.Lines.Count > 0);
        first = Math.Max(0, first);
        _loading = true; _frame.Maximum = Math.Max(0, manifest.Frames.Count - 1); _frame.Value = first; _loading = false;
        SelectFrame(first);
        AnnotationsChanged?.Invoke();
    }
    public void ReloadAnnotations()
    {
        if (_store == null) return;
        Pause(); SaveCurrent(); _annotations = _store.LoadAnnotations();
        DisplayAnnotation(); AnnotationsChanged?.Invoke();
    }

    private void SelectFrame(int index)
    {
        if (_manifest == null || _store == null || index == _index) return;
        try
        {
            SaveCurrent();
            var frame = _manifest.Frames[index];
            using var original = new Bitmap(_store.FramePath(frame));
            var image = new Bitmap(original);
            var old = _image.Image; _image.Image = image; old?.Dispose();
            _index = index;
            DisplayAnnotation();
        }
        catch (Exception ex) { _loading = false; Pause(); MessageBox.Show(this, ex.Message, "切换帧失败"); }
    }
    private void MoveFrame(int offset)
    { Pause(); _frame.Value = Math.Clamp(_frame.Value + offset, _frame.Minimum, _frame.Maximum); }
    private void ViewOriginal()
    {
        if (_image.Image == null) return;
        using var form = new Form { Text = "原尺寸截图", Size = new Size(1100, 800), StartPosition = FormStartPosition.CenterParent };
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        using var copy = new Bitmap(_image.Image);
        panel.Controls.Add(new PictureBox { Image = copy, SizeMode = PictureBoxSizeMode.AutoSize });
        form.Controls.Add(panel); form.ShowDialog(this);
    }
}
