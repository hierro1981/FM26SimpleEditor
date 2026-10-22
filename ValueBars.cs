using System.Globalization;

namespace FM26SimpleEditor;

public static class ValuePalette
{
    public static Color For(int value20, string key = "")
    {
        int level = Math.Clamp(value20, 1, 20);
        if (key is "injury_proneness" or "dirtiness") level = 21 - level;
        return level <= 5 ? Color.FromArgb(201, 68, 66) : level <= 10 ? Color.FromArgb(214, 166, 41) : level <= 15 ? Color.FromArgb(45, 153, 101) : Color.FromArgb(49, 119, 205);
    }
    public static string Legend => L10n.Get("Text_5ff89766ab1c");
}

public sealed class ValueBarCell : DataGridViewTextBoxCell
{
    public int NominalMaximum { get; set; } = 20;
    public ValueBarCell() { }
    public ValueBarCell(int maximum) { NominalMaximum = maximum; }
    public override object Clone() { var clone = (ValueBarCell)base.Clone(); clone.NominalMaximum = NominalMaximum; return clone; }

    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
        DataGridViewElementStates cellState, object? value, object? formattedValue, string? errorText,
        DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
    {
        base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle,
            paintParts & ~DataGridViewPaintParts.ContentForeground);
        if (!int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var number))
        {
            string missing = Convert.ToString(formattedValue) ?? "";
            TextRenderer.DrawText(graphics, missing.Length == 0 ? "—" : missing, cellStyle.Font, cellBounds, cellStyle.ForeColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            return;
        }
        string key = OwningRow?.DataBoundItem is EditableField field ? field.Key : OwningRow?.Tag as string ?? OwningColumn?.Tag as string ?? "";
        int displayed = NominalMaximum == 100 ? PreviewMath.Attribute20(number) : NominalMaximum == 20 ? Math.Clamp(number, 1, 20) : Math.Clamp((int)Math.Round(number * 20.0 / NominalMaximum), 1, 20);
        var bar = new Rectangle(cellBounds.X + 7, cellBounds.Y + 5, Math.Max(1, cellBounds.Width - 14), 5);
        using var background = new SolidBrush(Color.FromArgb(229, 233, 238));
        using var fill = new SolidBrush(ValuePalette.For(displayed, key));
        graphics.FillRectangle(background, bar);
        graphics.FillRectangle(fill, bar.X, bar.Y, (int)Math.Round(bar.Width * Math.Clamp(number / (double)NominalMaximum, 0, 1)), bar.Height);
        var text = new Rectangle(cellBounds.X + 6, cellBounds.Y + 12, Math.Max(1, cellBounds.Width - 12), Math.Max(1, cellBounds.Height - 14));
        bool selected = (cellState & DataGridViewElementStates.Selected) != 0;
        TextRenderer.DrawText(graphics, number.ToString(CultureInfo.InvariantCulture), cellStyle.Font, text,
            selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

public sealed class ValueMeter : Control
{
    private int? number;
    public int Maximum { get; set; } = 200;
    public int? Value { get => number; set { number = value; Invalidate(); } }
    public ValueMeter() { Height = 8; Width = 80; DoubleBuffered = true; TabStop = false; }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        using var empty = new SolidBrush(Color.FromArgb(229, 233, 238));
        args.Graphics.FillRectangle(empty, ClientRectangle);
        if (number is not int value || value < 0) return;
        int score = Math.Clamp((int)Math.Round(value * 20.0 / Maximum), 1, 20);
        using var color = new SolidBrush(ValuePalette.For(score));
        args.Graphics.FillRectangle(color, 0, 0, (int)(Width * Math.Clamp(value / (double)Maximum, 0, 1)), Height);
    }
    public static Control Wrap(NumericUpDown input, ValueMeter meter)
    {
        var layout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, Width = input.Width + 4, Height = 45, Margin = input.Margin };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        meter.Dock = DockStyle.Fill; input.Dock = DockStyle.Fill;
        layout.Controls.Add(meter, 0, 0); layout.Controls.Add(input, 0, 1);
        return layout;
    }
}


// Numeric cell value remains available to accessibility tools; glyphs show half stars.
public sealed class StarRatingCell : DataGridViewTextBoxCell
{
    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
        DataGridViewElementStates cellState, object? value, object? formattedValue, string? errorText,
        DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
    {
        if (value is not double rating)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            return;
        }
        base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle,
            paintParts & ~DataGridViewPaintParts.ContentForeground);
        var saved = graphics.Save();
        graphics.SetClip(Rectangle.Intersect(cellBounds, clipBounds));
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float size = Math.Min(22, Math.Min(cellBounds.Height - 8, (cellBounds.Width - 12) / 5f - 2));
        if (size <= 0) { graphics.Restore(saved); return; }
        using var empty = new SolidBrush(Color.FromArgb(222, 222, 217));
        using var gold = new SolidBrush(Color.FromArgb(230, 175, 48));
        using var outline = new Pen(Color.FromArgb(155, 133, 78));
        for (int star = 0; star < 5; star++)
        {
            float left = cellBounds.X + 6 + star * (size + 2), top = cellBounds.Y + (cellBounds.Height - size) / 2;
            var points = Enumerable.Range(0, 10).Select(i =>
            {
                double angle = -Math.PI / 2 + i * Math.PI / 5;
                double radius = i % 2 == 0 ? size * .48 : size * .21;
                return new PointF(left + size / 2 + (float)(Math.Cos(angle) * radius), top + size / 2 + (float)(Math.Sin(angle) * radius));
            }).ToArray();
            graphics.FillPolygon(empty, points);
            var clipping = graphics.Save();
            graphics.IntersectClip(new RectangleF(left, top, size * (float)Math.Clamp(rating - star, 0, 1), size));
            graphics.FillPolygon(gold, points); graphics.Restore(clipping);
            graphics.DrawPolygon(outline, points);
        }
        graphics.Restore(saved);
    }
}
