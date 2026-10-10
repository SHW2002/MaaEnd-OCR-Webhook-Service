namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class DatasetBrowser
{
    private void TogglePlayback()
    {
        if (_manifest == null) return;
        if (_timer.Enabled) { Pause(); return; }
        if (_frame.Value == _frame.Maximum) _frame.Value = 0;
        _play.Text = "暂停";
        ScheduleNext();
    }
    private void ScheduleNext()
    {
        if (_manifest == null || _index < 0 || _index + 1 >= _manifest.Frames.Count) { Pause(); return; }
        _timer.Interval = Math.Max(1, (int)Math.Min(int.MaxValue, _manifest.Frames[_index + 1].OffsetMs - _manifest.Frames[_index].OffsetMs));
        _timer.Start();
    }
    private void AdvancePlayback()
    { _timer.Stop(); _frame.Value = Math.Min(_frame.Maximum, _frame.Value + 1); ScheduleNext(); }
    private void Pause() { _timer.Stop(); _play.Text = "播放"; }
}
