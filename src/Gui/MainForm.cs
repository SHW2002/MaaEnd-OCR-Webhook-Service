using MaaEnd_Log_Retransmitter.App;
using MaaEnd_Log_Retransmitter.Infrastructure;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class MainForm : Form
{
    private readonly RunPage _run;
    private readonly RecordingPage _record;
    private readonly EvaluationPage _evaluate;
    private readonly ToolStripStatusLabel _status = new("就绪") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private Arguments _config;
    private CancellationTokenSource? _cancellation;
    private Task? _operation;
    private bool _closing;

    public MainForm(bool debug)
    {
        Text = "MaaEnd 日志转发器";
        Size = new Size(1120, 800);
        MinimumSize = new Size(940, 740);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Theme.Font;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        try { _config = ConfigStore.Load(); }
        catch (Exception ex) { _config = new() { SearchText = "运行日志" }; _status.Text = ex.Message; }
        _run = new RunPage(debug);
        _record = new RecordingPage();
        _evaluate = new EvaluationPage();
        _run.StartRequested += () =>
        {
            var debugEnabled = _run.DebugEnabled;
            StartOperation(token => LiveRecognitionRunner.RunAsync(_config, debugEnabled, token));
        };
        _record.StartRequested += StartRecording;
        _record.SaveRequested += () => SaveRecording();
        _record.DeleteRequested += () => DeleteRecording();
        _evaluate.EvaluateRequested += () =>
        {
            var store = _evaluate.Store!; var productionTiming = _evaluate.ProductionTiming;
            var workerCount = _evaluate.WorkerCount;
            var progress = new Progress<string>(SetStatus);
            StartOperation(async token =>
            {
                var folder = await Datasets.DatasetEvaluator.EvaluateAsync(store, productionTiming, progress, token, workerCount);
                _evaluate.ShowReport(folder);
            });
        };
        _run.StopRequested += StopOperation;
        _record.StopRequested += StopOperation;
        _evaluate.StopRequested += StopOperation;
        _record.SettingsRequested += EditConfiguration;
        InitializeLayout();
        Logger.MessageLogged += AppendLog;
        FormClosing += OnClosing;
        FormClosed += (_, _) => Logger.MessageLogged -= AppendLog;
    }

    private void EditConfiguration()
    {
        if (_operation is { IsCompleted: false }) { SetStatus("请先停止当前任务再修改配置"); return; }
        using var dialog = new ConfigurationForm(_config);
        if (dialog.ShowDialog(this) == DialogResult.OK) { _config = dialog.Configuration; SetStatus("配置已保存"); }
    }

    private async void StartOperation(Func<CancellationToken, Task> work, Action? finished = null)
    {
        if (_operation is { IsCompleted: false }) return;
        _cancellation = new CancellationTokenSource();
        SetBusy(true);
        // Run CPU work off the WinForms thread; marshal only progress and final UI changes.
        _operation = Task.Run(() => work(_cancellation.Token));
        try { await _operation; SetStatus("任务已完成"); }
        catch (OperationCanceledException) { SetStatus("任务已停止"); }
        catch (Exception ex) { SetStatus("失败：" + ex.Message); Logger.Error(ex.ToString()); }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            SetBusy(false);
            finished?.Invoke();
            if (_closing) Close();
        }
    }

    private void SetBusy(bool busy)
    { _run.SetBusy(busy); _record.SetBusy(busy); _evaluate.SetBusy(busy); }
    private void StopOperation() { _cancellation?.Cancel(); SetStatus("正在停止…"); }
    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (_cancellation == null)
        {
            if (!ResolveRecordingOnClose()) { e.Cancel = true; _closing = false; return; }
            try { _evaluate.SaveAnnotations(); }
            catch (Exception ex) { e.Cancel = true; MessageBox.Show(this, ex.Message, "标注保存失败"); }
            return;
        }
        e.Cancel = true; _closing = true; StopOperation();
    }
    private void SetStatus(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => SetStatus(message)); return; }
        _status.Text = message.Length > 85 ? message[..85] + "…" : message;
    }
    private void AppendLog(string message)
    {
        if (IsDisposed || !IsHandleCreated) return;
        BeginInvoke(() => _run.AppendLog(message));
    }
}
