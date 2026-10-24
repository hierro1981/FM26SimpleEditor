using System.Drawing.Drawing2D;

namespace FM26SimpleEditor;

// A native grid button (including keyboard activation) with a drawn copy icon.
// Its underlying text supplies the accessible action name, not a font-dependent glyph.
public sealed class CopyNameCell : DataGridViewButtonCell
{
    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
        DataGridViewElementStates cellState, object? value, object? formattedValue, string? errorText,
        DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
    {
        base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
            cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.ContentForeground);
        if (!paintParts.HasFlag(DataGridViewPaintParts.ContentForeground)) return;
        float scale = Math.Min(cellBounds.Width / 34f, cellBounds.Height / 38f);
        float x = cellBounds.X + (cellBounds.Width - 16 * scale) / 2;
        float y = cellBounds.Y + (cellBounds.Height - 17 * scale) / 2;
        var state = graphics.Save();
        try
        {
            graphics.SetClip(Rectangle.Intersect(clipBounds, cellBounds));
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(SystemColors.ControlText, 1.5f * scale);
            // Draw only the visible edges of the rear page, leaving no background patch.
            graphics.DrawLines(pen, new PointF[] {
                new(x + 5*scale, y + 5*scale), new(x + 5*scale, y),
                new(x + 15*scale, y), new(x + 15*scale, y + 12*scale), new(x + 10*scale, y + 12*scale)
            });
            graphics.DrawRectangle(pen, x, y + 5*scale, 10*scale, 12*scale);
        }
        finally { graphics.Restore(state); }
    }
}
