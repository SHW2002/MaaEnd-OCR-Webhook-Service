using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class AnnotationOverviewForm : Form
{
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    public int FramePosition { get; private set; } = -1;

    public AnnotationOverviewForm(IReadOnlyList<AnnotationText> texts)
    {
        Text = "全部标注文本"; Size = new Size(1000, 680); MinimumSize = new Size(760, 480);
        StartPosition = FormStartPosition.CenterParent; Font = Theme.Font;
        BackColor = Theme.Background; Padding = new Padding(10);
        _grid.Columns.Add("Frame", "首次帧"); _grid.Columns.Add("Time", "日志时间"); _grid.Columns.Add("Content", "完整正文");
        foreach (DataGridViewColumn column in _grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
        _grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        Theme.Grid(_grid);
        foreach (var text in texts)
        {
            var row = _grid.Rows[_grid.Rows.Add(text.FramePosition + 1, text.Line.TimeText, text.Line.Content)];
            row.Tag = text.FramePosition;
        }
        _grid.CellDoubleClick += (_, args) => { if (args.RowIndex >= 0) LocateFrame(); };
        var locate = Ui.Button("定位帧", LocateFrame); Theme.Primary(locate); locate.Enabled = texts.Count > 0;
        var close = Ui.Button("关闭", () => Close()); CancelButton = close;
        var actions = Ui.Row(locate, close); actions.Dock = DockStyle.Bottom;
        actions.Padding = new Padding(0, 6, 0, 0);
        var explanation = Ui.Label($"{texts.Count:N0} 条不同的标注文本 · 同时间、同正文只展示一次 · 双击可定位画面");
        explanation.Dock = DockStyle.Top; explanation.Padding = new Padding(0, 0, 0, 8);
        Controls.Add(UiLayout.Surface(_grid, 1)); Controls.Add(actions); Controls.Add(explanation);
    }

    private void LocateFrame()
    {
        if (_grid.CurrentRow?.Tag is not int position) return;
        FramePosition = position; DialogResult = DialogResult.OK;
    }
}
