using System.Drawing.Drawing2D;
using System.Globalization;

namespace FM26SimpleEditor;

public sealed class StarControl : Control
{
    private double? rating;
    public double? Rating { get => rating; set { rating = value; AccessibleName = value is null ? L10n.Get("Text_6b52861acb61") : L10n.Format("Text_fd123f397c97", value); Invalidate(); } }
    public StarControl() { DoubleBuffered = true; Height = 34; Width = 180; }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        var graphics = args.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float size = Math.Min(Height - 4, Width / 5f - 3);
        if (size <= 0) return;
        using var outline = new Pen(Color.FromArgb(155, 133, 78), 1f);
        using var empty = new SolidBrush(Color.FromArgb(232, 231, 225));
        using var gold = new SolidBrush(Color.FromArgb(230, 175, 48));
        for (var star = 0; star < 5; star++)
        {
            var left = star * (size + 3) + 1;
            var top = (Height - size) / 2;
            var points = Enumerable.Range(0, 10).Select(point =>
            {
                double angle = -Math.PI / 2 + point * Math.PI / 5;
                double radius = point % 2 == 0 ? size * .48 : size * .21;
                return new PointF(left + size / 2 + (float)(Math.Cos(angle) * radius), top + size / 2 + (float)(Math.Sin(angle) * radius));
            }).ToArray();
            graphics.FillPolygon(empty, points);
            float fraction = (float)Math.Clamp((rating ?? 0) - star, 0, 1);
            if (fraction > 0)
            {
                var state = graphics.Save();
                graphics.SetClip(new RectangleF(left, top, size * fraction, size));
                graphics.FillPolygon(gold, points);
                graphics.Restore(state);
            }
            graphics.DrawPolygon(outline, points);
        }
    }
}

public sealed class PitchControl : Control
{
    private readonly ToolTip tip = new();
    private readonly Dictionary<string, RectangleF> hitAreas = new();
    private Dictionary<string, int> ratings = new();
    private bool italian = true;
    private string lastKey = "";
    private string selectedPosition = "";
    public bool SelectionEnabled { get; set; }
    public event Action<string>? PositionSelected;
    public string SelectedPosition { get => selectedPosition; set { selectedPosition = value; Invalidate(); } }
    private static readonly (string Key, float X, float Y)[] Nodes =
    [
        ("GK", .5f, .92f), ("SW", .5f, .80f),
        ("DL", .18f, .69f), ("DC", .5f, .69f), ("DR", .82f, .69f),
        ("WBL", .14f, .54f), ("DM", .5f, .54f), ("WBR", .86f, .54f),
        ("ML", .18f, .40f), ("MC", .5f, .40f), ("MR", .82f, .40f),
        ("AML", .18f, .24f), ("AMC", .5f, .24f), ("AMR", .82f, .24f), ("STC", .5f, .09f)
    ];

    public PitchControl() { DoubleBuffered = true; MinimumSize = new Size(140, 120); Dock = DockStyle.Fill; AccessibleName = L10n.Get("Text_edcf091dd19a"); }
    public void SetPositions(IEnumerable<EditableField> fields, bool useItalian)
    {
        ratings = fields.ToDictionary(field => field.Key, field => field.Value);
        italian = useItalian; lastKey = ""; tip.SetToolTip(this, ""); Invalidate();
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        var g = args.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float h = Math.Max(0, Math.Min(Height - 28, (Width - 44) / .65f));
        float w = h * .65f;
        var pitch = new RectangleF((Width - w) / 2, (Height - h) / 2, w, h);
        if (pitch.Width < 50 || pitch.Height < 50) return;
        using var grass = new SolidBrush(Color.FromArgb(35, 94, 66));
        using var stripe = new SolidBrush(Color.FromArgb(40, 104, 72));
        using var line = new Pen(Color.FromArgb(150, 220, 226, 215), 1.4f);
        g.FillRectangle(grass, pitch);
        for (int i = 0; i < 10; i += 2) g.FillRectangle(stripe, pitch.X, pitch.Y + i * pitch.Height / 10, pitch.Width, pitch.Height / 10);
        g.DrawRectangle(line, pitch.X, pitch.Y, pitch.Width, pitch.Height);
        g.DrawLine(line, pitch.Left, pitch.Top + pitch.Height / 2, pitch.Right, pitch.Top + pitch.Height / 2);
        float radius = pitch.Width * .15f;
        g.DrawEllipse(line, pitch.Left + pitch.Width / 2 - radius, pitch.Top + pitch.Height / 2 - radius, radius * 2, radius * 2);
        g.DrawRectangle(line, pitch.Left + pitch.Width * .23f, pitch.Top, pitch.Width * .54f, pitch.Height * .15f);
        g.DrawRectangle(line, pitch.Left + pitch.Width * .23f, pitch.Bottom - pitch.Height * .15f, pitch.Width * .54f, pitch.Height * .15f);
        hitAreas.Clear();
        float dot = Math.Clamp(pitch.Height * .088f, 12, 36);
        using var dotFont = new Font(Font.FontFamily, Math.Clamp(dot * .25f, 5.5f, 10f), FontStyle.Bold);
        foreach (var (key, x, y) in Nodes)
        {
            var rating = ratings.GetValueOrDefault(key, 1);
            var box = new RectangleF(pitch.Left + x * pitch.Width - dot / 2, pitch.Top + y * pitch.Height - dot / 2, dot, dot);
            hitAreas[key] = box;
            var color = SelectionEnabled ? key == SelectedPosition ? Color.Gold : Color.LightSteelBlue : rating >= 18 ? Color.FromArgb(89, 220, 126) : rating >= 15 ? Color.FromArgb(244, 203, 82) : rating >= 12 ? Color.FromArgb(223, 151, 76) : Color.FromArgb(73, 112, 93);
            using var fill = new SolidBrush(color);
            using var border = new Pen(Color.FromArgb(220, 242, 232), rating >= 15 ? 2 : 1);
            g.FillEllipse(fill, box); g.DrawEllipse(border, box);
            var text = Labels.PositionCode(key, italian);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var ink = new SolidBrush(SelectionEnabled || rating >= 12 ? Color.FromArgb(20, 39, 30) : Color.FromArgb(212, 226, 216));
            g.DrawString(text, dotFont, ink, box, format);
        }
    }
    protected override void OnMouseMove(MouseEventArgs args)
    {
        base.OnMouseMove(args);
        var key = hitAreas.FirstOrDefault(pair => pair.Value.Contains(args.Location)).Key ?? "";
        if (key == lastKey) return;
        lastKey = key;
        Cursor = SelectionEnabled && key.Length > 0 ? Cursors.Hand : Cursors.Default;
        tip.SetToolTip(this, key.Length == 0 ? "" : SelectionEnabled ? Labels.Position(key, italian) : $"{Labels.Position(key, italian)}: {ratings.GetValueOrDefault(key, 1)}/20");
    }
    protected override void OnMouseDown(MouseEventArgs args)
    {
        base.OnMouseDown(args);
        if (!SelectionEnabled || args.Button != MouseButtons.Left) return;
        var key = hitAreas.FirstOrDefault(pair => pair.Value.Contains(args.Location)).Key;
        if (key is null) return;
        SelectedPosition = key; PositionSelected?.Invoke(key);
    }
    protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }
}

public sealed class PlayerPreview : UserControl
{
    private readonly Label title = new() { AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
    private readonly Label subtitle = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label currentText = new() { AutoSize = true };
    private readonly Label potentialText = new() { AutoSize = true };
    private readonly Label referenceText = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(980, 0) };
    private readonly Label naturalText = new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8), MaximumSize = new Size(340, 0) };
    private readonly Label legend = new() { Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(340, 0), ForeColor = Color.DimGray, Padding = new Padding(8) };
    private readonly StarControl absoluteCurrent = new();
    private readonly StarControl absolutePotential = new();
    private readonly StarControl leagueCurrent = new();
    private readonly StarControl leaguePotential = new();
    private readonly Label leagueText = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(980, 0) };
    private readonly StarControl currentStars = new();
    private readonly StarControl potentialStars = new();
    private readonly ValueMeter currentMeter = new() { Maximum = 200, Width = 240 };
    private readonly ValueMeter potentialMeter = new() { Maximum = 200, Width = 240 };
    private readonly PitchControl pitch = new();
    private readonly FlowLayoutPanel groups = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(5), WrapContents = true };
    private readonly CheckBox showHidden = new() { AutoSize = true, Text = L10n.Get("Text_dd2a6a16b813"), Margin = new Padding(14, 8, 0, 0) };
    private readonly Font valueFont = new("Segoe UI", 10, FontStyle.Bold);
    private PlayerDetail? detail;
    private bool italian;

    public PlayerPreview()
    {
        Size = new Size(1100, 700);
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        header.Controls.Add(title); header.Controls.Add(subtitle);
        var ratings = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6, Margin = new Padding(0, 0, 0, 12) };
        ratings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); ratings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); ratings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); ratings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ratings.Controls.Add(MeterLabel(currentMeter,currentText), 0, 0); ratings.Controls.Add(currentStars, 1, 0);
        ratings.Controls.Add(MeterLabel(potentialMeter,potentialText), 2, 0); ratings.Controls.Add(potentialStars, 3, 0);
        ratings.Controls.Add(referenceText, 0, 1); ratings.SetColumnSpan(referenceText, 4);
        ratings.Controls.Add(new Label { Text = L10n.Get("Text_d4992007fff7"), AutoSize = true }, 0, 2); ratings.Controls.Add(absoluteCurrent, 1, 2);
        ratings.Controls.Add(new Label { Text = L10n.Get("Text_afb8d72126a8"), AutoSize = true }, 2, 2); ratings.Controls.Add(absolutePotential, 3, 2);
        ratings.Controls.Add(new Label { Text = L10n.Get("Text_28666d70b8cd"), AutoSize = true }, 0, 3); ratings.Controls.Add(leagueCurrent, 1, 3);
        ratings.Controls.Add(new Label { Text = L10n.Get("Text_ce9bb84c3e8c"), AutoSize = true }, 2, 3); ratings.Controls.Add(leaguePotential, 3, 3);
        ratings.Controls.Add(leagueText, 0, 4); ratings.SetColumnSpan(leagueText, 4);
        ratings.Controls.Add(showHidden, 0, 5); ratings.SetColumnSpan(showHidden, 4);
        for (int i = 0; i < 6; i++) ratings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var split = new SplitContainer { Size = new Size(1000, 500), Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterDistance = 345 };
        var field = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        field.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); field.RowStyles.Add(new RowStyle(SizeType.AutoSize)); field.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        field.Controls.Add(pitch, 0, 0); field.Controls.Add(naturalText, 0, 1); field.Controls.Add(legend, 0, 2);
        field.MinimumSize = new Size(0, 380); split.Panel1.AutoScroll = true;
        split.Panel1.Controls.Add(field); split.Panel2.Controls.Add(groups);
        root.Controls.Add(header, 0, 0); root.Controls.Add(ratings, 0, 1); root.Controls.Add(split, 0, 2);
        Controls.Add(root);
        showHidden.CheckedChanged += (_, _) => RebuildAttributes();
        groups.SizeChanged += (_, _) => ResizeGroups();
        L10n.Capture(this);
    }

    public void ShowPlayer(PlayerDetail selected, int ca, int pa, DateTime? birth, bool useItalian, double? squadMean, int cohortCount)
    {
        L10n.Apply(this, useItalian);
        detail = selected; italian = useItalian;
        title.Text = selected.Player.Name;
        string birthText = birth?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";
        subtitle.Text = $"{selected.Player.Team}  •  {birthText}  •  {(italian ? L10n.Get("Text_b4fff3cd3890", true) : L10n.Get("Text_b4fff3cd3890", false))}";
        var cs = PreviewMath.Stars(ca, squadMean); var ps = PreviewMath.Stars(pa > 0 ? pa : null, squadMean);
        currentText.Text = $"{(italian ? L10n.Get("Text_5fd3d93232e0", true) : L10n.Get("Text_5fd3d93232e0", false))}  {ca}/200  •  {(cs.HasValue ? cs.Value.ToString("0.0") + "/5" : L10n.Get("Text_eb34623b71cd"))}";
        potentialText.Text = $"{(italian ? L10n.Get("Text_ce82fff32927", true) : L10n.Get("Text_ce82fff32927", false))}  {(pa > 0 ? pa + "/200" : L10n.Get("Text_eb34623b71cd"))}  •  {(ps.HasValue ? ps.Value.ToString("0.0") + "/5" : L10n.Get("Text_eb34623b71cd"))}";
        currentStars.Rating = cs; potentialStars.Rating = ps;
        absoluteCurrent.Rating = PreviewMath.AbsoluteStars(ca); absolutePotential.Rating = PreviewMath.AbsoluteStars(pa);
        leagueCurrent.Rating = PreviewMath.Stars(ca, selected.League.Mean); leaguePotential.Rating = PreviewMath.Stars(pa > 0 ? pa : null, selected.League.Mean);
        leagueText.Text = L10n.Backend(selected.League.Label) + (selected.League.Mean.HasValue ? L10n.Format("Text_1a58f46e0409", selected.League.Mean, selected.League.Count) : L10n.Get("Text_15b3e2d192ce")) + L10n.Get("Text_57a6677642b5");
        currentMeter.Value = ca; potentialMeter.Value = pa >= 0 ? pa : null;
        referenceText.Text = squadMean.HasValue
            ? italian ? L10n.FormatIn(true, "Text_c88205316415", squadMean, cohortCount) : L10n.FormatIn(false, "Text_c88205316415", squadMean, cohortCount)
            : italian ? L10n.Get("Text_5014a12ba8f5", true) : L10n.Get("Text_5014a12ba8f5", false);
        showHidden.Text = italian ? L10n.Get("Text_dd2a6a16b813", true) : L10n.Get("Text_dd2a6a16b813", false);
        pitch.SetPositions(selected.Positions, italian);
        var natural = selected.Positions.Where(field => field.Value >= 18).OrderByDescending(field => field.Value).Select(field => Labels.Position(field.Key, italian));
        naturalText.Text = (italian ? L10n.Get("Text_dd7c9737f87c", true) : L10n.Get("Text_dd7c9737f87c", false)) + (natural.Any() ? string.Join(", ", natural) : "—");
        legend.Text = italian ? L10n.Get("Text_e64996006057", true) : L10n.Get("Text_e64996006057", false);
        RebuildAttributes();
    }

    private void RebuildAttributes()
    {
        if (detail is null) return;
        groups.SuspendLayout();
        foreach (Control control in groups.Controls.Cast<Control>().ToArray()) control.Dispose();
        groups.Controls.Clear();
        foreach (var key in new[] { "technical", "mental", "physical", "goalkeeping", "feet", "hidden" })
        {
            if (key == "hidden" && !showHidden.Checked) continue;
            var fields = Labels.Alphabetical(detail.Attributes.Where(field => Labels.Group(field.Key) == key), italian).ToArray();
            if (fields.Length == 0) continue;
            int rowHeight = Math.Max(40, TextRenderer.MeasureText("Ag", Font).Height + 17);
            var box = new GroupBox { Text = Labels.GroupTitle(key, italian), Width = 260, Height = 48 + fields.Length * rowHeight, Margin = new Padding(5), Padding = new Padding(8) };
            var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, ColumnHeadersVisible = false, BorderStyle = BorderStyle.None, BackgroundColor = SystemColors.Window, ScrollBars = ScrollBars.None, TabStop = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowTemplate = { Height = rowHeight } };
            grid.Columns.Add(new DataGridViewTextBoxColumn { AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Width = 65, CellTemplate = new ValueBarCell(20), DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Font = valueFont }, SortMode = DataGridViewColumnSortMode.NotSortable });
            foreach (var field in fields)
            {
                int value = PreviewMath.Attribute20(field.Value);
                int row = grid.Rows.Add(Labels.Attribute(field.Key, italian), value);
                grid.Rows[row].Tag = field.Key;
                grid.Rows[row].Cells[1].Style.ForeColor = ValuePalette.For(value,field.Key);
                grid.Rows[row].Cells[0].ToolTipText = Labels.Attribute(field.Key, italian);
                grid.Rows[row].Cells[1].ToolTipText = $"{field.Value} → {value}/20";
            }
            grid.ClearSelection(); box.Controls.Add(grid); groups.Controls.Add(box);
        }
        ResizeGroups(); groups.ResumeLayout();
    }

    private void ResizeGroups()
    {
        int width = Math.Max(240, groups.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 16);
        int columns = width >= 790 ? 3 : width >= 510 ? 2 : 1;
        int cardWidth = Math.Max(230, width / columns - 12);
        foreach (Control card in groups.Controls) card.Width = cardWidth;
    }

    private static Control MeterLabel(ValueMeter meter, Label label)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(meter); panel.Controls.Add(label); return panel;
    }

    protected override void Dispose(bool disposing) { if (disposing) valueFont.Dispose(); base.Dispose(disposing); }
}


