namespace MaaEnd_Log_Retransmitter.Gui;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(244, 246, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(219, 225, 233);
    public static readonly Color Text = Color.FromArgb(35, 45, 62);
    public static readonly Color Muted = Color.FromArgb(103, 115, 133);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color Selection = Color.FromArgb(230, 239, 255);
    public static readonly Color Canvas = Color.FromArgb(234, 238, 244);
    public static readonly Font Font = new("Microsoft YaHei UI", 9);

    public static void Grid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Background;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        grid.DefaultCellStyle.SelectionBackColor = Selection;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
    }

    public static void Primary(Button button)
    {
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderColor = Accent;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
    }
}
