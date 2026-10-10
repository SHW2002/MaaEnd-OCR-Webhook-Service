namespace MaaEnd_Log_Retransmitter.Tests;

internal sealed class TestWindow : Form
{
    public TestWindow()
    {
        Text = "MaaEnd OCR integration " + Guid.NewGuid().ToString("N");
        ClientSize = new Size(700, 230); StartPosition = FormStartPosition.Manual;
        Location = new Point(-1800, -1800); BackColor = Color.White; DoubleBuffered = true;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var font = new Font("Microsoft YaHei", 16, FontStyle.Regular, GraphicsUnit.Pixel);
        e.Graphics.DrawString("运行日志", font, Brushes.Black, 20, 20);
        e.Graphics.DrawString("12:00:00", font, Brushes.Black, 20, 80);
        e.Graphics.DrawString("任务开始：测试任务", font, Brushes.Black, 170, 80);
        e.Graphics.DrawString("12:00:01", font, Brushes.Black, 20, 130);
        e.Graphics.DrawString("任务完成：测试任务", font, Brushes.Black, 170, 130);
    }
}
