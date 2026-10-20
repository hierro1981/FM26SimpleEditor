namespace FM26SimpleEditor;

public sealed class ComparisonPanel : UserControl
{
    private readonly ComboBox source = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175 };
    private readonly TextBox query = new() { Width = 190, PlaceholderText = L10n.Get("Text_fa15392c881e") };
    private readonly SearchablePlayerPicker candidates = new() { Width = 320 };
    private readonly CheckBox raw = new() { Text = L10n.Get("Text_562a75697840"), AutoSize = true };
    private readonly CheckBox hidden = new() { Text = L10n.Get("Text_3da863f9136d"), AutoSize = true };
    private readonly CheckBox showPitch = new() { Text = L10n.Get("Text_39b226b0bc64"), Checked = true, AutoSize = true };
    private readonly Button remove = new() { Text = L10n.Get("Text_8b7aacbe906e"), AutoSize = true };
    private readonly Button clear = new() { Text = L10n.Get("Text_d7b494e94601"), AutoSize = true };
    private readonly Label info = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(1150, 0) };
    private readonly Label primaryName = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Label otherName = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Label primaryMeta = new() { AutoSize = true };
    private readonly Label otherMeta = new() { AutoSize = true };
    private readonly StarControl primaryStars = new();
    private readonly StarControl otherStars = new();
    private readonly StarControl primaryPotential = new();
    private readonly StarControl otherPotential = new();
    private readonly PitchControl primaryPitch = new() { MinimumSize = new Size(240, 210) };
    private readonly PitchControl otherPitch = new() { MinimumSize = new Size(240, 210) };
    private readonly TabControl views = new() { Dock = DockStyle.Fill };
    private readonly TabPage attributesTab = new(L10n.Get("Text_83f29bd14a6e"));
    private readonly TabPage reportTab = new(L10n.Get("Text_e50b26ad4723"));
    private readonly RoleReportPanel report = new();
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, ScrollBars = ScrollBars.Both, BackgroundColor = SystemColors.Window, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToResizeRows = false };
    private readonly Dictionary<uint, PlayerDetail> cache = new();
    private Func<uint, Task<PlayerDetail>>? loader;
    private IReadOnlyList<PlayerRow> catalog = [];
    private IReadOnlySet<uint> pool = new HashSet<uint>();
    private PlayerDetail? primary;
    private PlayerDetail? other;
    private int currentCa, currentPa;
    private bool italian = true, dirty, updating;
    private int version;
    private uint? preferredUid;
    public event Action<uint>? RemoveRequested;
    public event Action? ClearRequested;

    public ComparisonPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 5 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 320)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var toolbar = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize)); toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        source.Items.AddRange([L10n.Get("Text_db56c5c85177"), L10n.Get("Text_ea4bae741bfb")]); source.SelectedIndex = 0;
        candidates.Dock = DockStyle.Fill;
        toolbar.Controls.Add(source, 0, 0); toolbar.Controls.Add(query, 1, 0); toolbar.Controls.Add(candidates, 2, 0);
        var options = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, WrapContents = false };
        options.Controls.AddRange([remove, clear, showPitch, raw, hidden]); toolbar.Controls.Add(options, 0, 1); toolbar.SetColumnSpan(options, 3);
        var headers = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
        headers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); headers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        headers.Controls.Add(Header(primaryName, primaryMeta, primaryStars, primaryPotential), 0, 0);
        headers.Controls.Add(Header(otherName, otherMeta, otherStars, otherPotential), 1, 0);
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.Controls.Add(primaryPitch, 0, 0); fields.Controls.Add(otherPitch, 1, 0);
        attributesTab.Controls.Add(grid); reportTab.Controls.Add(report); views.TabPages.AddRange([attributesTab, reportTab]);
        root.Controls.Add(toolbar, 0, 0); root.Controls.Add(info, 0, 1); root.Controls.Add(headers, 0, 2); root.Controls.Add(fields, 0, 3); root.Controls.Add(views, 0, 4);
        void FitFields()
        {
            bool attributesVisible = views.SelectedTab is null || views.SelectedTab == attributesTab;
            bool showFields = attributesVisible && showPitch.Checked;
            fields.Visible = showFields;
            showPitch.Enabled = raw.Enabled = hidden.Enabled = attributesVisible;
            root.RowStyles[3].Height = showFields ? Math.Clamp(ClientSize.Height * .39f, 160 * DeviceDpi / 96f, 340 * DeviceDpi / 96f) : 0;
        }
        Load += (_, _) => FitFields();
        VisibleChanged += (_, _) => { if (Visible) FitFields(); };
        views.HandleCreated += (_, _) => FitFields();
        SizeChanged += (_, _) => FitFields(); showPitch.CheckedChanged += (_, _) => FitFields();
        views.SelectedIndexChanged += (_, _) => { FitFields(); Render(); };
        primaryPitch.MinimumSize = otherPitch.MinimumSize = Size.Empty;
        Controls.Add(root);
        FitFields();
        source.SelectedIndexChanged += (_, _) => RebuildPlayerChoices(); query.TextChanged += (_, _) => RebuildPlayerChoices();
        candidates.SelectedIndexChanged += async (_, _) => { if (!updating) await LoadReference(); };
        raw.CheckedChanged += (_, _) => Render(); hidden.CheckedChanged += (_, _) => Render();
        remove.Click += (_, _) => { if (candidates.SelectedItem is PlayerChoice choice) RemoveRequested?.Invoke(choice.Uid); };
        clear.Click += (_, _) => ClearRequested?.Invoke();
        L10n.Capture(this);
    }

    private static Control Header(Label name, Label meta, StarControl stars, StarControl potential)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var ratings = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        stars.Width = potential.Width = 145;
        ratings.Controls.AddRange([new Label { Text = "CA", AutoSize = true },stars,new Label { Text = "PA", AutoSize = true },potential]);
        panel.Controls.AddRange([name, meta, ratings]); return panel;
    }

    public void ClearCache() { ++version; cache.Clear(); other = null; }
    public void SetContext(PlayerDetail? current, int ca, int pa, bool hasEdits, IReadOnlyList<PlayerRow> players,
        IReadOnlySet<uint> comparisonPool, bool useItalian, Func<uint, Task<PlayerDetail>> load)
    {
        updating = true; L10n.Apply(this, useItalian); updating = false;
        candidates.RefreshLanguage();
        primary = current; currentCa = ca; currentPa = pa; dirty = hasEdits; catalog = players; pool = comparisonPool;
        italian = useItalian; loader = load;
        RebuildPlayerChoices();
    }

    public void PreferReference(uint uid)
    {
        SetPreferredReference(uid); RebuildPlayerChoices();
    }

    public void SetPreferredReference(uint uid)
    {
        preferredUid = uid;
        bool wasUpdating = updating; updating = true;
        query.Clear(); source.SelectedIndex = 1;
        updating = wasUpdating;
    }

    private async void RebuildPlayerChoices()
    {
        if (updating) return;
        ++version;
        uint? selected = preferredUid ?? (candidates.SelectedItem as PlayerChoice)?.Uid; preferredUid = null;
        updating = true;
        if (primary is null) candidates.SetItems([],null);
        if (primary is not null)
        {
            var p = primary.Player;
            var text = query.Text.Trim();
            var choices = catalog.Where(row => row.Uid != p.Uid &&
                (source.SelectedIndex == 1 ? pool.Contains(row.Uid) : p.TeamId.HasValue ? row.TeamId == p.TeamId : p.ClubUid.HasValue && row.ClubUid == p.ClubUid) &&
                (text.Length == 0 || row.Name.Contains(text, StringComparison.OrdinalIgnoreCase) || row.Uid.ToString().Contains(text) || (row.Id?.ToString() ?? "").Contains(text)))
                .OrderBy(row => PositionScheme.Rank(row.MainPosition)).ThenBy(row => row.Name, Labels.Comparer(italian)).ToList();
            candidates.SetItems(choices.Select(row => new PlayerChoice(row.Uid,
                $"{Labels.PositionCode(row.MainPosition, italian)} · {row.Name} · {row.Team}",
                $"{row.Name} {row.Team} {row.Id} {row.Uid}")).ToList(),selected);
        }
        updating = false;
        await LoadReference();
    }

    private async Task LoadReference()
    {
        int request = ++version;
        other = null; Render();
        if (candidates.SelectedItem is not PlayerChoice choice || loader is null) return;
        otherName.Text = L10n.Get("Text_9648e3497415");
        try
        {
            if (!cache.TryGetValue(choice.Uid, out var loaded)) loaded = await loader(choice.Uid);
            if (request != version || IsDisposed) return;
            cache[choice.Uid] = loaded; other = loaded; Render();
        }
        catch (Exception exc) { if (request == version && !IsDisposed) info.Text = L10n.Get("Text_eaaf759afa09") + exc.Message; }
    }

    private void Render()
    {
        remove.Enabled = candidates.SelectedItem is PlayerChoice entry && pool.Contains(entry.Uid);
        clear.Enabled = pool.Count > 0;
        grid.Columns.Clear(); grid.Rows.Clear();
        primaryName.Text = primary is null ? L10n.Get("Text_8a9947ee8c97") : (views.SelectedTab == reportTab ? "" : "A · ") + primary.Player.Name;
        otherName.Text = other is null ? L10n.Get("Text_81fb6bf9caec") : (views.SelectedTab == reportTab ? "" : "B · ") + other.Player.Name;
        primaryMeta.Text = primary is null ? "" : $"{primary.Player.Team} · CA {currentCa} · PA {(currentPa >= 0 ? currentPa.ToString() : L10n.Get("Text_eb34623b71cd"))}";
        otherMeta.Text = other is null ? "" : $"{other.Player.Team} · CA {other.Player.Ca} · PA {other.Player.Pa?.ToString() ?? L10n.Get("Text_eb34623b71cd")}";
        List<PlayerRow> squad = primary is null ? [] : catalog.Where(row => row.Ca > 0 && (primary.Player.TeamId.HasValue ? row.TeamId == primary.Player.TeamId : primary.Player.ClubUid.HasValue && row.ClubUid == primary.Player.ClubUid)).ToList();
        double? mean = squad.Count >= 3 ? squad.Average(row => row.Ca) : null;
        primaryStars.Rating = PreviewMath.Stars(currentCa, mean); otherStars.Rating = PreviewMath.Stars(other?.Player.Ca, mean);
        primaryPotential.Rating = PreviewMath.Stars(currentPa > 0 ? currentPa : null,mean); otherPotential.Rating = PreviewMath.Stars(other?.Player.Pa,mean);
        attributesTab.Text = italian ? L10n.Get("Text_83f29bd14a6e", true) : L10n.Get("Text_83f29bd14a6e", false); reportTab.Text = italian ? L10n.Get("Text_e50b26ad4723", true) : L10n.Get("Text_e50b26ad4723", false);
        info.Text = L10n.Format("Text_994f045e8439", pool.Count, (dirty ? L10n.Get("Text_7c2ab2538f02") : L10n.Get("Text_d460db79e60b"))) + (views.SelectedTab == reportTab ? L10n.Get("Text_7318992da38f") : ValuePalette.Legend);
        if (views.SelectedTab == reportTab)
            info.Text = italian
                ? L10n.FormatIn(true, "Text_b820128ee005", pool.Count, (primary?.Player.Name ?? L10n.Get("Text_f183249bfc83", true)), (dirty ? L10n.Get("Text_1b1ef2d59209", true) : L10n.Get("Text_be6f556dde17", true)), (other?.Player.Name ?? L10n.Get("Text_eae7da9c773b", true)))
                : L10n.FormatIn(false, "Text_b820128ee005", pool.Count, (primary?.Player.Name ?? L10n.Get("Text_f183249bfc83", false)), (dirty ? L10n.Get("Text_1b1ef2d59209", false) : L10n.Get("Text_be6f556dde17", false)), (other?.Player.Name ?? L10n.Get("Text_eae7da9c773b", false)));
        primaryPitch.SetPositions(primary?.Positions ?? [], italian); otherPitch.SetPositions(other?.Positions ?? [], italian);
        report.SetContext(primary, other, italian);
        if (primary is null || other is null) return;
        int maximum = raw.Checked ? 100 : 20;
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? L10n.Get("Text_b9bb40edbe6e", true) : L10n.Get("Text_b9bb40edbe6e", false), Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? L10n.Get("Text_707eecb875fc", true) : L10n.Get("Text_707eecb875fc", false), Width = 260, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "A · " + primary.Player.Name, Width = 230, CellTemplate = new ValueBarCell(maximum), DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Δ A − B", Width = 95, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "B · " + other.Player.Name, Width = 230, CellTemplate = new ValueBarCell(maximum), DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? L10n.Get("Text_86e7925e4177", true) : L10n.Get("Text_86e7925e4177", false), Width = 150, SortMode = DataGridViewColumnSortMode.NotSortable });
        float[] weights = [13, 25, 19, 10, 19, 14];
        for (int i = 0; i < grid.Columns.Count; i++) { grid.Columns[i].FillWeight = weights[i]; grid.Columns[i].MinimumWidth = i == 1 ? 130 : i == 3 ? 80 : 90; }
        string Winner(int delta) => delta > 0 ? primary.Player.Name : delta < 0 ? other.Player.Name : italian ? L10n.Get("Text_1700f4df5398", true) : L10n.Get("Text_1700f4df5398", false);
        info.Text += $"   CA: {Winner(currentCa-other.Player.Ca)} ({currentCa-other.Player.Ca:+0;-0;0})";
        if (currentPa > 0 && other.Player.Pa is > 0) info.Text += $" · PA: {Winner(currentPa-other.Player.Pa.Value)} ({currentPa-other.Player.Pa.Value:+0;-0;0})";
        grid.RowTemplate.Height = 41;
        var otherFields = other.Attributes.ToDictionary(field => field.Key);
        var order = new[] { "technical", "mental", "physical", "goalkeeping", "feet", "hidden" };
        foreach (var group in order)
        foreach (var field in Labels.Alphabetical(primary.Attributes.Where(field => Labels.Group(field.Key) == group && (hidden.Checked || group != "hidden")), italian))
        {
            if (!otherFields.TryGetValue(field.Key, out var right)) continue;
            int a = raw.Checked ? field.Value : PreviewMath.Attribute20(field.Value);
            int b = raw.Checked ? right.Value : PreviewMath.Attribute20(right.Value);
            int qualityDelta = field.Key is "injury_proneness" or "dirtiness" ? b-a : a-b;
            int row = grid.Rows.Add(Labels.GroupTitle(group, italian), Labels.Attribute(field.Key, italian), a, PreviewMath.DeltaText(a-b, qualityDelta), b);
            grid.Rows[row].Tag = field.Key;
            grid.Rows[row].Cells[5].Value = Winner(qualityDelta);
            grid.Rows[row].Cells[5].Style.ForeColor = qualityDelta > 0 ? Color.SeaGreen : qualityDelta < 0 ? Color.Firebrick : Color.DimGray;
            grid.Rows[row].Cells[3].Style.ForeColor = qualityDelta > 0 ? Color.SeaGreen : qualityDelta < 0 ? Color.Firebrick : Color.DimGray;
            grid.Rows[row].Cells[3].ToolTipText = L10n.Format("Text_97ca235373f6", field.Value-right.Value);
        }
        grid.ClearSelection();
    }
}



