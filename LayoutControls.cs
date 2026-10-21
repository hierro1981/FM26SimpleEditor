namespace FM26SimpleEditor;

/// <summary>Native tab navigation with distinct selected/unselected headers.</summary>
public sealed class AccentTabControl : TabControl
{
    public AccentTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        Padding = new Point(9, 4);
        ItemSize = new Size(0, Font.Height + 10);
    }
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ItemSize = new Size(0, Font.Height + 10);
        Invalidate();
    }
    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e); Invalidate();
    }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= TabPages.Count) return;
        bool selected = e.Index == SelectedIndex;
        Color background = SystemInformation.HighContrast ? selected ? SystemColors.Highlight : SystemColors.Control :
            selected ? Color.FromArgb(31, 91, 153) : Color.FromArgb(224, 235, 247);
        Color foreground = !Enabled ? SystemColors.GrayText : SystemInformation.HighContrast ?
            selected ? SystemColors.HighlightText : SystemColors.ControlText : selected ? Color.White : Color.FromArgb(30, 57, 85);
        using var fill = new SolidBrush(background);
        e.Graphics.FillRectangle(fill, e.Bounds);
        using var border = new Pen(SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(133, 164, 193));
        e.Graphics.DrawRectangle(border, e.Bounds.X, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 1), Math.Max(0, e.Bounds.Height - 1));
        var text = Rectangle.Inflate(e.Bounds, -5, -2);
        TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, text, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (selected && Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, text, foreground, background);
        base.OnDrawItem(e);
    }
}

/// <summary>
/// Scrolls a content surface once the available area is below its minimum size.
/// Dock.Fill children do not extend a WinForms AutoScroll display rectangle.
/// </summary>
public sealed class ContentViewport : Panel
{
    private Control? content;
    private bool fitting;
    public ContentViewport(Control content, Size minimum)
    {
        this.content = content;
        Dock = DockStyle.Fill; AutoScroll = true;
        content.Dock = DockStyle.None; content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        content.MinimumSize = minimum; content.Margin = Padding.Empty; content.Location = Point.Empty;
        Controls.Add(content);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if (content is null || fitting) { base.OnLayout(e); return; }
        fitting = true;
        try
        {
            AutoScrollMinSize = content.MinimumSize;
            var size = new Size(Math.Max(content.MinimumSize.Width, ClientSize.Width), Math.Max(content.MinimumSize.Height, ClientSize.Height));
            if (content.Size != size) content.Size = size;
            base.OnLayout(e);
        }
        finally { fitting = false; }
    }
}
