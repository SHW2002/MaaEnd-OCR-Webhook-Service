using MaaEnd_Log_Retransmitter.Datasets;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class EventAnnotationsForm : Form
{
    private readonly DatasetStore _store;
    private readonly DatasetAnnotations _annotations;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
    private readonly CheckBox _reviewed = UiLayout.Check("会话真值已人工确认");
    public EventAnnotationsForm(DatasetStore store)
    {
        _store = store; _annotations = store.LoadAnnotations();
        Text = "会话事件真值"; Size = new Size(900, 600); StartPosition = FormStartPosition.CenterParent;
        Font = Theme.Font; BackColor = Theme.Background; Padding = new Padding(10);
        _grid.Columns.Add("Time", "日志时间"); _grid.Columns.Add("Content", "完整正文"); _grid.Columns.Add("Offset", "首次出现（毫秒）");
        _grid.Columns[1].FillWeight = 250;
        _grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        _grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        Theme.Grid(_grid);
        foreach (var item in _annotations.Events) _grid.Rows.Add(item.TimeText, item.Content, item.FirstVisibleMs);
        _reviewed.Checked = _annotations.EventsReviewed;
        Controls.Add(UiLayout.Surface(_grid, 1));
        var save = Ui.Button("保存", Save); Theme.Primary(save);
        var confirmation = Ui.Row(_reviewed, save); confirmation.Dock = DockStyle.Bottom; confirmation.Padding = new Padding(0, 8, 0, 0);
        Controls.Add(confirmation);
        var draft = Ui.Row(Ui.Button("从标注生成草稿", Draft), Ui.Label("同正文在不同时间应分别记录"));
        draft.Padding = new Padding(0, 0, 0, 8); Controls.Add(draft);
    }
    private void Draft()
    {
        var manifest = _store.LoadManifest();
        _grid.Rows.Clear();
        var seen = new HashSet<(string, string)>();
        foreach (var frame in manifest.Frames)
        {
            if (!_annotations.Frames.TryGetValue(frame.Index, out var annotation) || !annotation.Reviewed) continue;
            foreach (var line in annotation.Lines)
                if (seen.Add((line.TimeText, line.Content))) _grid.Rows.Add(line.TimeText, line.Content, frame.OffsetMs);
        }
        _reviewed.Checked = false;
    }
    private void Save()
    {
        try
        {
            _grid.EndEdit();
            var events = new List<ExpectedEvent>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                var time = row.Cells[0].Value?.ToString() ?? "";
                var content = row.Cells[1].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(content)) throw new InvalidDataException("事件正文不能为空");
                if (!double.TryParse(row.Cells[2].Value?.ToString(), out var offset) || !double.IsFinite(offset) || offset < 0)
                    throw new InvalidDataException("首次出现时间必须为非负毫秒数");
                events.Add(new(time, content, offset));
            }
            _annotations.Events = events; _annotations.EventsReviewed = _reviewed.Checked;
            _store.SaveAnnotations(_annotations); DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "标注需要修正"); }
    }
}
