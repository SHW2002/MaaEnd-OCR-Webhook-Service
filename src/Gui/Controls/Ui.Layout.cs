namespace MaaEnd_Log_Retransmitter.Gui;

internal static class UiLayout
{
    public static TableLayoutPanel Field(string label, Control input, params Control[] actions)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = actions.Length + 2,
            Padding = Padding.Empty, Margin = new Padding(0, 0, 0, 6) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var title = Ui.Label(label);
        title.Anchor = AnchorStyles.Left;
        title.Margin = new Padding(0, 0, 12, 0);
        input.Dock = DockStyle.Fill;
        row.Controls.Add(title, 0, 0); row.Controls.Add(input, 1, 0);
        for (var i = 0; i < actions.Length; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(actions[i], i + 2, 0);
        }
        return row;
    }

    public static TableLayoutPanel Stack(params Control[] controls)
    {
        var stack = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1,
            RowCount = controls.Length, Margin = Padding.Empty, Padding = Padding.Empty };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < controls.Length; i++)
        {
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controls[i].Dock = DockStyle.Fill;
            stack.Controls.Add(controls[i], 0, i);
        }
        return stack;
    }

    public static Panel Surface(Control content, int padding = 8)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(padding) };
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Paint += (_, e) => { using var pen = new Pen(Theme.Border); e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1); };
        return panel;
    }

    public static CheckBox Check(string text) => new() { Text = text, AutoSize = true,
        ForeColor = Theme.Text, Margin = new Padding(0, 5, 12, 0) };

    public static ToolStrip Toolbar(params ToolStripItem[] items)
    {
        var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Theme.Surface,
            ForeColor = Theme.Text, Padding = new Padding(6, 3, 6, 3), RenderMode = ToolStripRenderMode.System };
        toolbar.Items.AddRange(items);
        return toolbar;
    }

    public static ToolStripButton ToolButton(string text, Action action)
    {
        var button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text,
            Padding = new Padding(8, 3, 8, 3), ForeColor = Theme.Text };
        button.Click += (_, _) => action();
        return button;
    }
}
