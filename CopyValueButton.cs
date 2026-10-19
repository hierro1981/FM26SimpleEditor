using System.Drawing.Drawing2D;

namespace FM26SimpleEditor;

// Draw the standard two-page copy symbol at the current display scale.
public sealed class CopyValueButton : Button
{
    public CopyValueButton()
    {
        Size = new Size(29, 29); Margin = new Padding(3, 1, 8, 1);
        Text = ""; TabStop = true;
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        float scale = Math.Min(ClientSize.Width, ClientSize.Height) / 29f;
        float x = (ClientSize.Width - 16 * scale) / 2, y = (ClientSize.Height - 17 * scale) / 2;
        args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Enabled ? ForeColor : SystemColors.GrayText, 1.6f * scale);
        args.Graphics.DrawRectangle(pen, x + 5 * scale, y, 10 * scale, 12 * scale);
        using var fill = new SolidBrush(BackColor);
        args.Graphics.FillRectangle(fill, x, y + 5 * scale, 10 * scale, 12 * scale);
        args.Graphics.DrawRectangle(pen, x, y + 5 * scale, 10 * scale, 12 * scale);
    }
}
