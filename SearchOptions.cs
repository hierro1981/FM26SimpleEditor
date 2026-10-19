namespace FM26SimpleEditor;

public sealed class OptionalRangeFilter : GroupBox
{
    private readonly CheckBox active = new() { Text = L10n.Get("Text_6792c3433e1b"), AutoSize = true };
    private readonly NumericUpDown lower = new() { Width = 60 };
    private readonly NumericUpDown upper = new() { Width = 60 };
    private readonly int defaultMin;
    private readonly int defaultMax;
    private bool updating;
    public event EventHandler? Changed;
    public int? MinimumValue => active.Checked ? (int)lower.Value : null;
    public int? MaximumValue => active.Checked ? (int)upper.Value : null;
    public bool Active => active.Checked;

    public OptionalRangeFilter(string title, int allowedMin, int allowedMax, int initialMin, int initialMax, bool minimumOnly = false)
    {
        Text = title; Width = 245; Height = 83; Padding = new Padding(7); Margin = new Padding(4);
        defaultMin = initialMin; defaultMax = initialMax;
        lower.Minimum = upper.Minimum = allowedMin; lower.Maximum = upper.Maximum = allowedMax;
        lower.Value = initialMin; upper.Value = initialMax; lower.Enabled = upper.Enabled = false;
        var row = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        row.Controls.AddRange([active, new Label { Text = minimumOnly ? L10n.Get("Text_bcc1f408078e") : L10n.Get("Text_aa58b21b01d6"), AutoSize = true, Margin = new Padding(2, 5, 1, 0) }, lower]);
        if (!minimumOnly) row.Controls.AddRange([new Label { Text = L10n.Get("Text_ca978112ca1b"), AutoSize = true, Margin = new Padding(2, 5, 1, 0) }, upper]);
        Controls.Add(row);
        active.CheckedChanged += (_, _) => { lower.Enabled = upper.Enabled = active.Checked; Notify(); };
        lower.ValueChanged += (_, _) =>
        {
            if (updating) return;
            updating = true; if (lower.Value > upper.Value) upper.Value = lower.Value; updating = false; Notify();
        };
        upper.ValueChanged += (_, _) =>
        {
            if (updating) return;
            updating = true; if (upper.Value < lower.Value) lower.Value = upper.Value; updating = false; Notify();
        };
    }

    private void Notify() { if (!updating) Changed?.Invoke(this, EventArgs.Empty); }
    public void ResetFilter()
    {
        updating = true; active.Checked = false; lower.Value = defaultMin; upper.Value = defaultMax; updating = false; Notify();
    }
}

public sealed class SearchOptions : UserControl
{
    private sealed record Choice(string Key, string Label) { public override string ToString() => Label; }
    private readonly OptionalRangeFilter age = new(L10n.Get("Text_db2ed7c2417a"), 0, 120, 15, 50);
    private readonly OptionalRangeFilter ca = new(L10n.Get("Text_f6ab09c6bbfa"), 0, 255, 0, 200);
    private readonly OptionalRangeFilter pa = new(L10n.Get("Text_adcecb06f9f3"), 0, 255, 0, 200);
    private readonly OptionalRangeFilter height = new(L10n.Get("Text_34912a9e4c68"), 100, 250, 150, 210);
    private readonly OptionalRangeFilter growth = new(L10n.Get("Text_b1536395f037"), 0, 255, 10, 255, minimumOnly: true);
    private readonly ComboBox position = Dropdown();
    private readonly ComboBox level = Dropdown();
    private readonly ComboBox contract = Dropdown();
    private readonly ComboBox foot = Dropdown();
    private readonly ComboBox loan = Dropdown();
    private readonly ToolTip tips = new();
    private readonly FlowLayoutPanel cards = new() { AutoSize = false, AutoScroll = true, Dock = DockStyle.Fill, WrapContents = true, Padding = Padding.Empty };
    private bool updating;
    public event EventHandler? Changed;
    public int ActiveCount => new[] { age, ca, pa, height, growth }.Count(range => range.Active) +
        (position.SelectedIndex > 0 ? 1 : 0) + (contract.SelectedIndex > 0 ? 1 : 0) + (foot.SelectedIndex > 0 ? 1 : 0) + (loan.SelectedIndex > 0 ? 1 : 0);

    public SearchOptions()
    {
        AutoSize = false; Height = 190; Dock = DockStyle.Fill;
        var role = new GroupBox { Text = L10n.Get("Text_a95838c3f0ec"), Width = 330, Height = 83, Padding = new Padding(7), Margin = new Padding(4) };
        var row = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        row.Controls.Add(position, 0, 0); row.Controls.Add(level, 1, 0); role.Controls.Add(row);
        position.DropDownWidth = 285;
        level.Items.AddRange([new Choice("15", L10n.Get("Text_951b8d171625")), new Choice("12", L10n.Get("Text_4c099aaccac0"))]); level.SelectedIndex = 0; level.Enabled = false;
        contract.Items.AddRange([new Choice("all", L10n.Get("Text_7f65ef4af637")), new Choice("none", L10n.Get("Text_c4bdfd33d95d")), new Choice("expired", L10n.Get("Text_89bf53640b26")),
            new Choice("6m", L10n.Get("Text_f69457a73dd8")), new Choice("12m", L10n.Get("Text_d2e2201308d8")), new Choice("active", L10n.Get("Text_e070660e5c42")), new Choice("unknown", L10n.Get("Text_c7172fdd812d"))]);
        foot.Items.AddRange([new Choice("all", L10n.Get("Text_7f65ef4af637")), new Choice("left", L10n.Get("Text_94a4c8661be8")), new Choice("right", L10n.Get("Text_7acb74dfc936")), new Choice("both", L10n.Get("Text_c2acac38d378"))]);
        loan.Items.AddRange([new Choice("all", L10n.Get("Text_7f65ef4af637")), new Choice("yes", L10n.Get("Text_71a727f536e4")), new Choice("no", L10n.Get("Text_57e4df9fbdb4")), new Choice("unknown", L10n.Get("Text_c7172fdd812d"))]);
        contract.SelectedIndex = foot.SelectedIndex = loan.SelectedIndex = 0;
        cards.Controls.AddRange([age, role, ca, pa, Card(L10n.Get("Text_725424c55624"), contract, 260), height, growth, Card(L10n.Get("Text_ceaf655a3ef2"), foot), Card(L10n.Get("Text_c4ff98018ca7"), loan)]);
        Controls.Add(cards);
        tips.SetToolTip(growth, L10n.Get("Text_5764e89cef6f"));
        tips.SetToolTip(pa, L10n.Get("Text_0af945bd4a9b"));
        tips.SetToolTip(age, L10n.Get("Text_8ca49b27492a"));
        tips.SetToolTip(contract, L10n.Get("Text_d1cb94b7d991"));
        tips.SetToolTip(position, L10n.Get("Text_f81f2c0dbff0"));
        tips.SetToolTip(foot, L10n.Get("Text_0d15148c4e4a"));
        foreach (var range in new[] { age, ca, pa, height, growth }) range.Changed += (_, _) => Notify();
        foreach (var combo in new[] { position, level, contract, foot, loan }) combo.SelectedIndexChanged += (_, _) => { level.Enabled = position.SelectedIndex > 0; Notify(); };
        L10n.Capture(this, tips);
        SetLanguage(L10n.IsItalian);
    }

    // Measure the wrapped cards at the available width, independently of the previous height.
    // Keep a two-row viewport at narrow widths; additional rows remain scrollable.
    public int HeightForWidth(int width) => Math.Min(
        cards.GetPreferredSize(new Size(Math.Max(1, width - Padding.Horizontal), 0)).Height + Padding.Vertical,
        (int)Math.Ceiling(190 * DeviceDpi / 96f));

    private static ComboBox Dropdown() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, IntegralHeight = false, DropDownHeight = 280 };
    private static GroupBox Card(string title, Control child, int width = 245)
    {
        var card = new GroupBox { Text = title, Width = width, Height = 83, Padding = new Padding(8), Margin = new Padding(4) };
        card.Controls.Add(child); return card;
    }
    private static string Key(ComboBox combo) => (combo.SelectedItem as Choice)?.Key ?? "all";
    private void Notify() { if (!updating) Changed?.Invoke(this, EventArgs.Empty); }

    public void SetLanguage(bool italian)
    {
        updating = true;
        L10n.Apply(this, italian);
        foreach (var combo in new[] { level, contract, foot, loan })
            for (int i = 0; i < combo.Items.Count; i++)
                if (combo.Items[i] is Choice choice) combo.Items[i] = new Choice(choice.Key, L10n.Translate(choice.Label, italian));
        var old = Key(position);
        position.Items.Clear(); position.Items.Add(new Choice("", italian ? L10n.Get("Text_ae145daf7f24", true) : L10n.Get("Text_ae145daf7f24", false)));
        foreach (var key in PositionScheme.Order)
            position.Items.Add(new Choice(key, Labels.Position(key, italian)));
        position.SelectedIndex = Enumerable.Range(0, position.Items.Count).FirstOrDefault(index => ((Choice)position.Items[index]).Key == old);
        updating = false;
    }

    public SearchRequest Request(string text, string? exactTeam, string? partialTeam) => new()
    {
        Text = text, TeamExact = exactTeam, TeamContains = partialTeam,
        AgeMin = age.MinimumValue, AgeMax = age.MaximumValue,
        CaMin = ca.MinimumValue, CaMax = ca.MaximumValue, PaMin = pa.MinimumValue, PaMax = pa.MaximumValue,
        HeightMin = height.MinimumValue, HeightMax = height.MaximumValue, GrowthMin = growth.MinimumValue,
        Position = position.SelectedIndex > 0 ? Key(position) : null,
        PositionMin = Key(level) == "12" ? 12 : 15,
        Contract = Key(contract), Foot = Key(foot), Loan = Key(loan)
    };

    public void ResetFilters()
    {
        updating = true;
        foreach (var range in new[] { age, ca, pa, height, growth }) range.ResetFilter();
        position.SelectedIndex = contract.SelectedIndex = foot.SelectedIndex = loan.SelectedIndex = level.SelectedIndex = 0;
        updating = false; Notify();
    }

    protected override void Dispose(bool disposing) { if (disposing) tips.Dispose(); base.Dispose(disposing); }
}


