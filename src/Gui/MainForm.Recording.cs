using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class MainForm
{
    private PendingRecording? _pendingRecording;

    private void StartRecording()
    {
        if (_pendingRecording != null || _operation is { IsCompleted: false }) return;
        try
        {
            var recording = PendingRecording.Create(_record.OutputDirectory);
            _pendingRecording = recording;
            var interval = _record.IntervalMs; var scenario = _record.Scenario;
            var progress = new Progress<string>(SetStatus);
            var preview = new Progress<RecordingPreview>(_record.ShowPreview);
            _record.PrepareRecording();
            StartOperation(token => DatasetRecorder.RecordAsync(recording, _config, interval, scenario, progress, token, preview),
                () => CompleteRecording(recording));
        }
        catch (Exception ex) { SetStatus("录制失败：" + ex.Message); }
    }

    internal void CompleteRecording(PendingRecording recording)
    {
        _pendingRecording = recording;
        _record.SetPending(recording.FrameCount);
        ShowDevelopmentPage(false);
        if (recording.FrameCount > 0) SetStatus("录制已结束，请保存或删除本次录制");
    }

    private bool SaveRecording(bool openEvaluation = true)
    {
        if (_pendingRecording == null) return true;
        try
        {
            var directory = _pendingRecording.Save();
            _pendingRecording = null;
            _record.SetSaved(directory);
            SetStatus("录制已保存：" + DatasetPaths.Display(directory));
            if (openEvaluation)
            {
                ShowDevelopmentPage(true);
                _evaluate.LoadDataset(Path.Combine(directory, "dataset.json"));
            }
            return true;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "保存录制失败"); return false; }
    }

    private bool DeleteRecording()
    {
        if (_pendingRecording == null) return true;
        try
        {
            _pendingRecording.Delete();
            _pendingRecording = null;
            _record.SetDeleted();
            SetStatus("已删除本次录制");
            return true;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "删除录制失败"); return false; }
    }

    private bool ResolveRecordingOnClose()
    {
        if (_pendingRecording == null) return true;
        ShowDevelopmentPage(false);
        using var dialog = new RecordingDecisionForm(_pendingRecording.FrameCount > 0);
        return dialog.ShowDialog(this) switch
        {
            DialogResult.Yes => SaveRecording(openEvaluation: false),
            DialogResult.No => DeleteRecording(),
            _ => false
        };
    }
}
