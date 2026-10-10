namespace MaaEnd_Log_Retransmitter.Gui;

internal static class Ui
{
    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true,
            Padding = Padding.Empty, Margin = Padding.Empty, WrapContents = false };
        panel.Controls.AddRange(controls);
        return panel;
    }
    public static Button Button(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(56, 22),
            Padding = new Padding(7, 1, 7, 1), Margin = new Padding(0, 0, 6, 0),
            FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface, ForeColor = Theme.Text, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderColor = Theme.Border;
        button.FlatAppearance.MouseOverBackColor = Theme.Background;
        button.Click += (_, _) => action();
        return button;
    }
    public static Label Label(string text) => new() { Text = text, AutoSize = true,
        ForeColor = Theme.Muted, Margin = new Padding(0, 4, 8, 4) };
    public static TextBox Text(string value = "", int width = 280) => new() { Text = value, Width = width,
        BorderStyle = BorderStyle.FixedSingle, ForeColor = Theme.Text, Margin = new Padding(0, 3, 6, 3) };
    public static TabPage Tab(string title, Control content)
    {
        var tab = new TabPage(title) { Padding = new Padding(6), BackColor = Theme.Surface };
        content.Dock = DockStyle.Fill;
        tab.Controls.Add(content);
        return tab;
    }
    public static string? BrowseFolder(IWin32Window owner, string initial)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = initial, Description = "选择数据集保存目录", UseDescriptionForTitle = true };
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.SelectedPath : null;
    }
    public static void OpenFolder(string path)
        => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
}
