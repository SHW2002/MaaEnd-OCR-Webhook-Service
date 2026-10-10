namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed partial class DatasetBrowser
{
    private void InitializeLayout()
    {
        _anchor.Checked = true;
        _lines.Columns.Add("Time", "时间"); _lines.Columns.Add("Content", "日志正文");
        foreach (DataGridViewColumn column in _lines.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
        _lines.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        Theme.Grid(_lines);
        var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(960, 440),
            SplitterWidth = 6, FixedPanel = FixedPanel.Panel2, BackColor = Theme.Background };
        split.Panel1MinSize = 240; split.Panel2MinSize = 280; split.SplitterDistance = 580;
        split.Panel1.Controls.Add(_image);
        var annotations = split.Panel2;
        annotations.BackColor = Theme.Surface;
        annotations.Padding = new Padding(6, 0, 0, 0);
        annotations.Controls.Add(_lines);
        var editing = UiLayout.Field("场景", _scenario, _reviewed, _anchor,
            Ui.Button("保存标注", SaveWithFeedback), Ui.Button("重载标注", ReloadWithFeedback),
            Ui.Button("OCR 草稿", GenerateDraft));
        editing.Padding = new Padding(0, 0, 0, 6);
        _play = UiLayout.ToolButton("播放", TogglePlayback);
        var previous = UiLayout.ToolButton("上一帧", () => MoveFrame(-1));
        var next = UiLayout.ToolButton("下一帧", () => MoveFrame(1));
        var original = UiLayout.ToolButton("原尺寸", ViewOriginal);
        var navigation = UiLayout.Toolbar(_play, previous, next, new ToolStripSeparator(), original, new ToolStripSeparator(), _info);
        navigation.Dock = DockStyle.Bottom;
        _frame.Dock = DockStyle.Bottom; _frame.Height = 24;
        Controls.Add(split); Controls.Add(_frame); Controls.Add(navigation);
        Controls.Add(editing); Controls.Add(CreateAnnotationNavigation());
    }
}
