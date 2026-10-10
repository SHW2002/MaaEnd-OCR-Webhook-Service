using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class EvaluationPage : UserControl
{
    private readonly TextBox _path = Ui.Text("records");
    private readonly CheckBox _timing = UiLayout.Check("按正式频率采样");
    private readonly NumericUpDown _workers = new() { Minimum = 1, Maximum = ReplaySegments.MaximumWorkers,
        Value = ReplaySegments.DefaultWorkers, Width = 48, Margin = new Padding(0, 3, 8, 0), AccessibleName = "计算线程" };
    private readonly Button _evaluate;
    private readonly Button _stop;
    private readonly DatasetBrowser _browser = new() { Dock = DockStyle.Fill };
    private readonly Label _report = Ui.Label("打开数据集后，可浏览画面并确认标注");
    private readonly TableLayoutPanel _datasetActions;
    private readonly Button _events;
    private readonly Button _viewReport;
    private string? _reportDirectory;
    public DatasetStore? Store { get; private set; }
    public bool ProductionTiming => _timing.Checked;
    public int WorkerCount => (int)_workers.Value;
    public event Action? EvaluateRequested;
    public event Action? StopRequested;

    public EvaluationPage()
    {
        AutoScaleMode = AutoScaleMode.None;
        _timing.Checked = true;
        var modeTip = new ToolTip();
        modeTip.SetToolTip(_timing, "默认按正式运行的处理耗时和等待间隔采样；取消勾选将逐帧运行 OCR，耗时更长。");
        modeTip.SetToolTip(_workers, "按录制时间均分，每个线程评估一个连续片段；最后按时间顺序统一过滤和去重。设为 1 可连续回放整个会话并测量单独运行性能。");
        Disposed += (_, _) => modeTip.Dispose();
        _browser.AnnotationsChanged += UpdateDatasetSummary;
        _path.PlaceholderText = "选择录制的数据集";
        _evaluate = Ui.Button("开始评估", () =>
        {
            if (Store == null) return;
            try { _browser.SaveCurrent(); EvaluateRequested?.Invoke(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "标注保存失败"); }
        });
        Theme.Primary(_evaluate);
        _stop = Ui.Button("停止", () => StopRequested?.Invoke());
        _stop.Enabled = false; _evaluate.Enabled = false;
        _events = Ui.Button("事件真值", EditEvents);
        _viewReport = Ui.Button("查看报告", () =>
        {
            if (_reportDirectory == null) return;
            using var form = new ReportForm(_reportDirectory); form.ShowDialog(this);
        });
        _viewReport.Enabled = false;
        var actions = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 3, 0, 8) };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions.Controls.Add(Ui.Row(_evaluate, _stop, _timing, Ui.Label("线程"), _workers, _events), 0, 0); actions.Controls.Add(_viewReport, 1, 0);
        _datasetActions = UiLayout.Field("数据集", _path, Ui.Button("浏览", Browse),
            Ui.Button("载入", OpenDataset), Ui.Button("打开目录", OpenDirectory));
        _report.Dock = DockStyle.Bottom; _report.Padding = new Padding(2, 5, 0, 0);
        Controls.Add(UiLayout.Surface(_browser, 6)); Controls.Add(_report); Controls.Add(UiLayout.Stack(_datasetActions, actions));
    }
    public void LoadDataset(string path)
    {
        if (InvokeRequired) { BeginInvoke(() => LoadDataset(path)); return; }
        try
        {
            _browser.SaveCurrent();
            var store = DatasetStore.Open(DatasetPaths.Resolve(path));
            var manifest = store.LoadManifest();
            _browser.LoadDataset(store, manifest);
            Store = store; _path.Text = DatasetPaths.Display(store.DirectoryPath); _evaluate.Enabled = true;
            _reportDirectory = null; _viewReport.Enabled = false;
            UpdateDatasetSummary();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "打开数据集失败"); }
    }
    private void UpdateDatasetSummary()
        => _report.Text = $"{_browser.FrameCount:N0} 帧  ·  {_browser.AnnotationSummary}  ·  {_browser.AnnotationTextCount:N0} 条不同文本";
    private void EditEvents()
    {
        if (Store == null) return;
        try
        {
            _browser.SaveCurrent();
            using var form = new EventAnnotationsForm(Store); form.ShowDialog(this);
            _browser.ReloadAnnotations();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "事件标注失败"); }
    }
    public void SetBusy(bool busy)
    { _evaluate.Enabled = !busy && Store != null; _stop.Enabled = busy; _timing.Enabled = !busy;
        _workers.Enabled = !busy;
        _browser.Enabled = !busy; _datasetActions.Enabled = !busy; _events.Enabled = !busy; }
    public void SaveAnnotations() => _browser.SaveCurrent();
    public void ShowReport(string directory)
    {
        if (InvokeRequired) { BeginInvoke(() => ShowReport(directory)); return; }
        _reportDirectory = directory; _viewReport.Enabled = true; _report.Text = "评估已完成";
        using var form = new ReportForm(directory); form.ShowDialog(this);
    }
}
