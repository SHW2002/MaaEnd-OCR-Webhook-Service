using System.Text.Json;
using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class ConfigurationForm : Form
{
    public Arguments Configuration { get; }
    private readonly TextBox _body;
    public ConfigurationForm(Arguments config)
    {
        Configuration = JsonSerializer.Deserialize<Arguments>(JsonSerializer.Serialize(config))!;
        Text = "配置"; Size = new Size(720, 700); MinimumSize = new Size(560, 600);
        Font = Theme.Font; BackColor = Theme.Background; ForeColor = Theme.Text;
        StartPosition = FormStartPosition.CenterParent;
        var grid = new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = new LocalizedSettings(Configuration), ToolbarVisible = false,
            ViewBackColor = Theme.Surface, ViewForeColor = Theme.Text, LineColor = Theme.Border, HelpVisible = false };
        _body = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, Text = Configuration.WebhookBody,
            BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.Surface, ForeColor = Theme.Text };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(grid, 0, 0);
        layout.Padding = new Padding(10);
        layout.Controls.Add(Ui.Label("Webhook 请求体  ·  __CONTENT__ 正文 / __TIME__ 时间"), 0, 1);
        layout.Controls.Add(_body, 0, 2);
        var save = Ui.Button("保存", Save); Theme.Primary(save); AcceptButton = save;
        var buttons = Ui.Row(save, Ui.Button("取消", () => Close())); buttons.Padding = new Padding(0, 8, 0, 0);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
    }
    private void Save()
    {
        Configuration.WebhookBody = _body.Text;
        if (!Configuration.TryValidate(true, out var errors))
        { MessageBox.Show(this, string.Join("\n", errors), "配置需要修正"); return; }
        try { ConfigStore.Save(Configuration); DialogResult = DialogResult.OK; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "保存失败"); }
    }
}
