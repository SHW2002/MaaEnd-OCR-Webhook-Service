namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class ReportForm : Form
{
    public ReportForm(string directory)
    {
        Text = "数据集评估报告"; Size = new Size(1040, 660); StartPosition = FormStartPosition.CenterParent;
        Font = Theme.Font; BackColor = Theme.Background; Padding = new Padding(10);
        var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
            WordWrap = false, BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text,
            Font = new Font("Consolas", 9), Text = File.ReadAllText(Path.Combine(directory, "report.md")) };
        Controls.Add(UiLayout.Surface(text, 10));
        var actions = Ui.Row(Ui.Button("打开报告目录", () => Ui.OpenFolder(directory)), Ui.Label("逐帧明细：report.json"));
        actions.Padding = new Padding(0, 0, 0, 8); Controls.Add(actions);
    }
}
