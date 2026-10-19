using System.Drawing.Drawing2D;
using System.Globalization;

namespace FM26SimpleEditor;

public sealed class AttributeHistoryChart : Control
{
    public sealed record PointValue(double X, int Value, string Label, string Source, bool Current);
    public sealed record Series(string Key, string Label, Color Color, DashStyle Dash, List<PointValue> Points);
    private List<Series> series = [];
    private readonly List<(RectangleF Area, string Text)> hits = [];
    private readonly ToolTip tip = new();
    private int maximum = 20;
    private bool dated;
    private string lastTip = "";
    public AttributeHistoryChart() { Dock = DockStyle.Fill; DoubleBuffered = true; BackColor = Color.White; }
    public void SetSeries(List<Series> values, int upper, bool useDates)
    {
        series = values; maximum = upper; dated = useDates; hits.Clear(); lastTip = ""; tip.SetToolTip(this, ""); Invalidate();
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        hits.Clear();
        var g = args.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        var plot = new RectangleF(45*scale, 14*scale, Width-65*scale, Height-55*scale);
        if (plot.Width < 30 || plot.Height < 25) return;
        using var rule = new Pen(Color.FromArgb(224, 230, 236));
        using var ink = new SolidBrush(Color.DimGray);
        for (int i = 0; i <= 4; i++)
        {
            float y = plot.Bottom - plot.Height*i/4;
            g.DrawLine(rule, plot.Left, y, plot.Right, y);
            g.DrawString(Math.Round(maximum*i/4.0).ToString(CultureInfo.InvariantCulture), Font, ink, 3, y-Font.Height/2f);
        }
        var points = series.SelectMany(line => line.Points).ToList();
        if (points.Count == 0) { g.DrawString(L10n.Get("Text_01f602d60568"), Font, ink, plot.Left, plot.Top); return; }
        double first = points.Min(point => point.X), last = points.Max(point => point.X), span = last-first;
        foreach (var line in series)
        {
            bool unique = line.Points.Select(point => point.X).Distinct().Count() == line.Points.Count;
            PointF? previous = null;
            using var connection = new Pen(line.Color, 1.8f*scale) { DashStyle = line.Dash };
            using var fill = new SolidBrush(line.Color);
            using var border = new Pen(Color.Black, scale);
            foreach (var point in line.Points.OrderBy(point => point.X))
            {
                float x = span == 0 ? plot.Left+plot.Width/2 : plot.Left+(float)((point.X-first)/span)*plot.Width;
                float y = plot.Bottom-(float)Math.Clamp(point.Value/(double)maximum, 0, 1)*plot.Height;
                var at = new PointF(x,y);
                if (unique && previous is PointF before) g.DrawLine(connection, before, at);
                float radius = (point.Current ? 4 : 2.7f)*scale;
                g.FillEllipse(fill,x-radius,y-radius,2*radius,2*radius);
                if (point.Current) g.DrawEllipse(border,x-radius,y-radius,2*radius,2*radius);
                hits.Add((new RectangleF(x-7*scale,y-7*scale,14*scale,14*scale), $"{line.Label}: {point.Value} · {point.Label}\n{Path.GetFileName(point.Source)}"));
                previous = at;
            }
        }
        string Label(double x) => dated ? DateTime.FromOADate(x).ToString("dd/MM/yyyy") :
            points.First(point => point.X == x).Label;
        string left = Label(first), right = Label(last);
        g.DrawString(left,Font,ink,plot.Left,plot.Bottom+7*scale);
        if (first != last) g.DrawString(right,Font,ink,plot.Right-g.MeasureString(right,Font).Width,plot.Bottom+7*scale);
    }
    protected override void OnMouseMove(MouseEventArgs args)
    {
        base.OnMouseMove(args);
        string text = string.Join("\n", hits.Where(hit => hit.Area.Contains(args.Location)).Select(hit => hit.Text));
        if (text == lastTip) return;
        lastTip = text; tip.SetToolTip(this, text);
    }
    protected override void OnMouseLeave(EventArgs args) { base.OnMouseLeave(args); lastTip = ""; tip.SetToolTip(this, ""); }
    protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }
}

public sealed class HistoryPanel : UserControl
{
    private sealed record Choice(string Key, string Label) { public override string ToString() => Label; }
    private static readonly Color[] Colors = [Color.FromArgb(30,91,177), Color.FromArgb(188,55,38), Color.FromArgb(18,125,89), Color.FromArgb(129,63,166), Color.FromArgb(155,103,0), Color.FromArgb(0,128,147), Color.FromArgb(181,45,119), Color.FromArgb(77,83,94), Color.FromArgb(105,111,17), Color.FromArgb(109,67,43), Color.FromArgb(64,73,152), Color.FromArgb(139,63,74)];
    private static readonly DashStyle[] Dashes = [DashStyle.Solid, DashStyle.Dash, DashStyle.Dot, DashStyle.DashDot, DashStyle.DashDotDot];
    private readonly Label title = new() { AutoSize = true, Font = new Font("Segoe UI", 15, FontStyle.Bold), Text = L10n.Get("Text_f0cdee43e3b1") };
    private readonly Label info = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Button add = new() { Text = L10n.Get("Text_a439407283a1"), AutoSize = true };
    private readonly Button clear = new() { Text = L10n.Get("Text_2f17719ac845"), AutoSize = true };
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 245 };
    private readonly TextBox find = new() { Dock = DockStyle.Fill, PlaceholderText = L10n.Get("Text_d12e07ecdec1") };
    private readonly CheckedListBox attributes = new() { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true };
    private readonly Label count = new() { AutoSize = true };
    private readonly Button all = new() { Text = L10n.Get("Text_c543c235ece1"), AutoSize = true };
    private readonly Button none = new() { Text = L10n.Get("Text_aa201262e9a7"), AutoSize = true };
    private readonly CheckBox raw = new() { Text = L10n.Get("Text_00f23f53197c"), AutoSize = true };
    private readonly CheckBox current = new() { Text = L10n.Get("Text_f7642bcf0f8f"), Checked = true, AutoSize = true };
    private readonly CheckBox delta = new() { Text = L10n.Get("Text_7c00e624d516"), Checked = true, AutoSize = true };
    private readonly FlowLayoutPanel legend = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
    private readonly AttributeHistoryChart chart = new();
    private readonly DataGridView grid = new() { ReadOnly = true, Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, ScrollBars = ScrollBars.Both, BackgroundColor = SystemColors.Window, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None };
    private readonly List<string> sources = [];
    private readonly HashSet<string> selected = new(StringComparer.Ordinal) { "technique", "dribbling", "passing" };
    private readonly Dictionary<string, (Color Color, DashStyle Dash)> styles = new(StringComparer.Ordinal);
    private Func<uint, string[], Task<HistoryResult>>? loader;
    private PlayerDetail? primary;
    private HistoryResult? result;
    private List<string> available = [];
    private bool italian = true, updating, preferInternal = true;
    private int version;

    public HistoryPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 4, ColumnCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var tools = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        mode.Items.AddRange([L10n.Get("Text_6efbb5528c52"), L10n.Get("Text_920a8419e478")]); mode.SelectedIndex = 0;
        tools.Controls.AddRange([mode, raw, current, delta, add, clear]);
        var body = new SplitContainer { Size = new Size(1100,550), Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterDistance = 245 };
        var selector = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        selector.RowStyles.Add(new RowStyle(SizeType.AutoSize)); selector.RowStyles.Add(new RowStyle(SizeType.AutoSize)); selector.RowStyles.Add(new RowStyle(SizeType.AutoSize)); selector.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true }; buttons.Controls.AddRange([all,none]);
        selector.Controls.Add(find,0,0); selector.Controls.Add(buttons,0,1); selector.Controls.Add(count,0,2); selector.Controls.Add(attributes,0,3);
        body.Panel1.Controls.Add(selector);
        var views = new SplitContainer { Size = new Size(850,550), Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };
        var graph = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        graph.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); graph.RowStyles.Add(new RowStyle(SizeType.Absolute,65)); graph.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        graph.Controls.Add(legend,0,0); graph.Controls.Add(chart,0,1); views.Panel1.Controls.Add(graph); views.Panel2.Controls.Add(grid); body.Panel2.Controls.Add(views);
        root.Controls.Add(title,0,0); root.Controls.Add(tools,0,1); root.Controls.Add(info,0,2); root.Controls.Add(body,0,3);
        root.SizeChanged += (_, _) => info.MaximumSize = new Size(Math.Max(150,root.ClientSize.Width-root.Padding.Horizontal-12),0);
        Controls.Add(root);
        add.Click += async (_, _) =>
        {
            if (primary is null) return;
            using var dialog = new OpenFileDialog { Filter = L10n.Get("Text_b67eeb7fa174"), Multiselect = true, Title = L10n.Get("Text_46fe5e750735") };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var paths = sources.Concat(dialog.FileNames).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (paths.Count > 20) { MessageBox.Show(L10n.Get("Text_0e1ce3bc5ee8")); return; }
            sources.Clear(); sources.AddRange(paths); preferInternal = false; await LoadHistory();
        };
        clear.Click += async (_, _) => { sources.Clear(); await LoadHistory(); };
        mode.SelectedIndexChanged += (_, _) => { if (!updating) { preferInternal = mode.SelectedIndex == 0; RebuildAvailable(); Render(); } };
        raw.CheckedChanged += (_, _) => Render(); current.CheckedChanged += (_, _) => Render(); delta.CheckedChanged += (_, _) => Render();
        find.TextChanged += (_, _) => RebuildList();
        all.Click += (_, _) => { foreach (Choice choice in attributes.Items) selected.Add(choice.Key); RebuildList(); Render(); };
        none.Click += (_, _) => { selected.Clear(); RebuildList(); Render(); };
        attributes.ItemCheck += (_, args) =>
        {
            if (updating || attributes.Items[args.Index] is not Choice choice) return;
            if (args.NewValue == CheckState.Checked) selected.Add(choice.Key); else selected.Remove(choice.Key);
            if (IsHandleCreated) BeginInvoke(new Action(Render));
        };
        L10n.Capture(this);
    }

    public void ClearSources()
    {
        ++version; sources.Clear(); result = null; primary = null; available.Clear(); preferInternal = true; RebuildList(); Render();
    }
    public void SetLanguage(bool useItalian) { updating = true; L10n.Apply(this, useItalian); updating = false; italian = useItalian; RebuildAvailable(); Render(); }
    public async void SetContext(PlayerDetail? player, bool useItalian, Func<uint,string[],Task<HistoryResult>> load)
    {
        bool same = ReferenceEquals(primary,player);
        SetLanguage(useItalian); primary = player; italian = useItalian; loader = load;
        if (same && result is not null) { RebuildAvailable(); Render(); return; }
        await LoadHistory();
    }
    private async Task LoadHistory()
    {
        int request = ++version;
        if (primary is null || loader is null) { result = null; RebuildAvailable(); Render(); return; }
        result = null; RebuildAvailable(); Render(); SetEnabled(false); info.Text = L10n.Get("Text_5b61f03e7658");
        try
        {
            var loaded = await loader(primary.Player.Uid,sources.ToArray());
            if (request != version || IsDisposed) return;
            result = loaded; updating = true;
            mode.SelectedIndex = preferInternal && loaded.EmbeddedAvailable ? 0 : 1;
            updating = false; RebuildAvailable(); Render();
        }
        catch (Exception exc) { if (request == version && !IsDisposed) info.Text = L10n.Get("Text_3689b6415fd0") + exc.Message; }
        finally { if (request == version && !IsDisposed) SetEnabled(true); }
    }
    private void SetEnabled(bool ready)
    {
        add.Enabled = ready && primary is not null; clear.Enabled = ready && sources.Count > 0;
        mode.Enabled = raw.Enabled = attributes.Enabled = current.Enabled = delta.Enabled = all.Enabled = none.Enabled = ready && result is not null;
    }
    private List<HistoryPoint> DataPoints() => result is null ? [] : mode.SelectedIndex == 0 ? result.EmbeddedPoints : result.Points;
    private void RebuildAvailable()
    {
        available = DataPoints().SelectMany(point => point.Attributes.Keys).Distinct(StringComparer.Ordinal).OrderBy(key => Labels.Attribute(key,italian),Labels.Comparer(italian)).ToList();
        foreach (string key in available.OrderBy(key => key,StringComparer.Ordinal))
            if (!styles.ContainsKey(key)) { int i = styles.Count; styles[key] = (Colors[i % Colors.Length], Dashes[(i / Colors.Length) % Dashes.Length]); }
        RebuildList();
    }
    private void RebuildList()
    {
        updating = true; attributes.BeginUpdate(); attributes.Items.Clear();
        foreach (string key in available.Where(key => Labels.Attribute(key,italian).Contains(find.Text.Trim(),StringComparison.OrdinalIgnoreCase) || key.Contains(find.Text.Trim(),StringComparison.OrdinalIgnoreCase)))
            attributes.Items.Add(new Choice(key,Labels.Attribute(key,italian)), selected.Contains(key));
        attributes.EndUpdate(); updating = false;
    }
    private void Render()
    {
        if (IsDisposed) return;
        title.Text = L10n.Get("Text_f0cdee43e3b1") + (primary is null ? "" : " · " + primary.Player.Name);
        grid.Columns.Clear(); grid.Rows.Clear(); chart.SetSeries([],20,false);
        foreach (Control control in legend.Controls.Cast<Control>().ToArray()) control.Dispose(); legend.Controls.Clear();
        var keys = available.Where(selected.Contains).ToList(); count.Text = L10n.Format("Text_46c06b548bb1", keys.Count, available.Count);
        current.Visible = mode.SelectedIndex == 0;
        if (result is null)
        {
            info.Text = primary is null ? L10n.Get("Text_302b8eb70bbc") : L10n.Get("Text_e2003fd16f1e");
            SetEnabled(true); return;
        }
        bool embedded = mode.SelectedIndex == 0;
        var points = DataPoints().OrderBy(point => embedded ? point.Sequence.ToString("D6") : point.Date,StringComparer.Ordinal).ThenBy(point => point.Source).ToList();
        bool duplicates = !embedded && points.GroupBy(point => point.Date).Any(group => group.Count()>1);
        info.Text = (embedded ? L10n.Backend(result.EmbeddedMessage) : L10n.Backend(result.Message) + " " + L10n.Backend(result.EmbeddedMessage)) + L10n.Get("Text_d765d2266795") +
            (embedded && current.Checked ? L10n.Get("Text_bba3193922f9") : "") +
            (duplicates ? L10n.Get("Text_9e4d513ec09b") : "") +
            (result.Warnings.Count == 0 ? "" : L10n.Get("Text_6ba40634b2ee") + string.Join(" | ",result.Warnings.Select(L10n.Backend)));
        HistoryPoint? currentPoint = embedded && current.Checked && primary is not null ? result.Points.FirstOrDefault(point => point.Source == result.EmbeddedPoints.FirstOrDefault()?.Source) : null;
        if (currentPoint is not null) points.Add(currentPoint);
        AddColumn(embedded ? L10n.Get("Text_1a9c88f4cf2e") : L10n.Get("Text_39b33a354458"),150);
        AddColumn(L10n.Get("Text_2fb609839b7f"),230);
        foreach (string key in keys)
        {
            var column = AddColumn(Labels.Attribute(key,italian),135,raw.Checked ? 100 : 20);
            column.HeaderCell.Style.ForeColor = styles[key].Color; column.Tag = key;
            if (delta.Checked) AddColumn("Δ " + Labels.Attribute(key,italian),90);
            var item = new Label { Text = Labels.Attribute(key,italian) + " · " + L10n.Get("Dash_" + styles[key].Dash), AutoSize = true, ForeColor = styles[key].Color, Margin = new Padding(6,4,10,4) };
            legend.Controls.Add(item);
        }
        AddColumn("CA",75,200); AddColumn("PA",75,200);
        var lines = keys.ToDictionary(key => key,key => new List<AttributeHistoryChart.PointValue>(),StringComparer.Ordinal);
        var before = new Dictionary<string,int>(StringComparer.Ordinal);
        for (int index = 0; index < points.Count; index++)
        {
            var point = points[index]; bool now = ReferenceEquals(point,currentPoint);
            DateTime? date = DateTime.TryParseExact(point.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed) ? parsed : null;
            string label = embedded ? now ? L10n.Get("Text_d39045327c8b") : L10n.Format("Text_0468a5223610", point.Sequence) : date?.ToString("dd/MM/yyyy") ?? "—";
            var cells = new List<object?> { now && date.HasValue ? L10n.Format("Text_d47f73a65981", date) : label, Path.GetFileName(point.Source) };
            foreach (string key in keys)
            {
                int? value = point.Attributes.TryGetValue(key,out int stored) ? raw.Checked ? stored : PreviewMath.Attribute20(stored) : null;
                cells.Add(value);
                if (delta.Checked) cells.Add(value.HasValue && before.TryGetValue(key,out int previous) && !duplicates ? value.Value-previous : null);
                if (value.HasValue)
                {
                    before[key] = value.Value;
                    double x = embedded ? index+1 : date?.ToOADate() ?? index+1;
                    lines[key].Add(new(x,value.Value,label,point.Source,now));
                }
            }
            cells.Add(point.Ca); cells.Add(point.Pa);
            int row = grid.Rows.Add(cells.ToArray());
            grid.Rows[row].Cells[1].ToolTipText = point.Source + (point.IdentityNote.Length>0 ? "\n"+L10n.Backend(point.IdentityNote) : "");
            if (now) grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(238,244,251);
        }
        int maximum = raw.Checked ? lines.Values.SelectMany(line => line).Any(point => point.Value>100) ? 125 : 100 : 20;
        chart.SetSeries(keys.Select(key => new AttributeHistoryChart.Series(key,Labels.Attribute(key,italian),styles[key].Color,styles[key].Dash,lines[key])).ToList(),maximum,!embedded);
        grid.ClearSelection();
    }
    private DataGridViewColumn AddColumn(string label, int width, int? maximum = null)
    {
        var column = new DataGridViewTextBoxColumn { HeaderText = label, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable };
        if (maximum.HasValue) column.CellTemplate = new ValueBarCell(maximum.Value);
        column.DefaultCellStyle.NullValue = "—";
        if (grid.Columns.Count >= 2) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns.Add(column); grid.RowTemplate.Height = 41; return column;
    }
}

