namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class RecordingDecisionForm : Form
{
    public RecordingDecisionForm(bool canSave)
    {
        Text = "处理本次录制"; ClientSize = new Size(410, 108);
        Font = Theme.Font; BackColor = Theme.Background; ForeColor = Theme.Text;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; Padding = new Padding(12);
        var message = Ui.Label(canSave ? "本次录制尚未保存，请选择如何处理。" : "本次录制没有可用帧，可以删除或取消关闭。");
        message.Dock = DockStyle.Top;
        var save = Ui.Button("保存", () => DialogResult = DialogResult.Yes);
        Theme.Primary(save); save.Enabled = canSave;
        var delete = Ui.Button("删除", () => DialogResult = DialogResult.No);
        var cancel = Ui.Button("取消关闭", () => DialogResult = DialogResult.Cancel);
        CancelButton = cancel;
        if (canSave) AcceptButton = save;
        var actions = Ui.Row(save, delete, cancel); actions.Dock = DockStyle.Bottom;
        Controls.Add(message); Controls.Add(actions);
    }
}
