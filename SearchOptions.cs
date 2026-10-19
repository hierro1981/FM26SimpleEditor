namespace FM26SimpleEditor;

public sealed class OptionalRangeFilter : GroupBox
{
    private readonly CheckBox active = new() { Text = "Usa", AutoSize = true };
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
        row.Controls.AddRange([active, new Label { Text = minimumOnly ? "almeno" : "da", AutoSize = true, Margin = new Padding(2, 5, 1, 0) }, lower]);
        if (!minimumOnly) row.Controls.AddRange([new Label { Text = "a", AutoSize = true, Margin = new Padding(2, 5, 1, 0) }, upper]);
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
    private readonly OptionalRangeFilter age = new("Età (anni)", 0, 120, 15, 50);
    private readonly OptionalRangeFilter ca = new("Abilità attuale · CA", 0, 255, 0, 200);
    private readonly OptionalRangeFilter pa = new("Abilità potenziale · PA", 0, 255, 0, 200);
    private readonly OptionalRangeFilter height = new("Altezza (cm)", 100, 250, 150, 210);
    private readonly OptionalRangeFilter growth = new("Margine PA − CA minimo", 0, 255, 10, 255, minimumOnly: true);
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
        var role = new GroupBox { Text = "Posizione e familiarità", Width = 330, Height = 83, Padding = new Padding(7), Margin = new Padding(4) };
        var row = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        row.Controls.Add(position, 0, 0); row.Controls.Add(level, 1, 0); role.Controls.Add(row);
        position.DropDownWidth = 285;
        level.Items.AddRange([new Choice("15", "Bravo (15+)"), new Choice("12", "Può giocare (12+)")]); level.SelectedIndex = 0; level.Enabled = false;
        contract.Items.AddRange([new Choice("all", "Tutti"), new Choice("none", "Senza contratto rilevato"), new Choice("expired", "Scaduto"),
            new Choice("6m", "Scadenza entro 6 mesi"), new Choice("12m", "Scadenza entro 12 mesi"), new Choice("active", "In corso, scadenza nota"), new Choice("unknown", "Dato non disponibile")]);
        foot.Items.AddRange([new Choice("all", "Tutti"), new Choice("left", "Sinistro dominante"), new Choice("right", "Destro dominante"), new Choice("both", "Entrambi forti (15+)")]);
        loan.Items.AddRange([new Choice("all", "Tutti"), new Choice("yes", "In prestito"), new Choice("no", "Non in prestito"), new Choice("unknown", "Dato non disponibile")]);
        contract.SelectedIndex = foot.SelectedIndex = loan.SelectedIndex = 0;
        cards.Controls.AddRange([age, role, ca, pa, Card("Contratto", contract, 260), height, growth, Card("Piede", foot), Card("Prestito", loan)]);
        Controls.Add(cards);
        tips.SetToolTip(growth, "Usa solo il valore minimo: differenza PA meno CA. I potenziali non noti sono esclusi.");
        tips.SetToolTip(pa, "Intervallo della PA numerica. Potenziali non disponibili o espressi con codice negativo sono esclusi quando il filtro è attivo.");
        tips.SetToolTip(age, "Età alla data della partita. Se la nascita non è leggibile, il giocatore è escluso soltanto quando il filtro età è attivo.");
        tips.SetToolTip(contract, "Scadenze dalla data della partita, estremi inclusi. Entro 12 mesi include anche i primi 6. Senza: nessun contratto rilevato e nessuna squadra associata. I dati non disponibili sono separati.");
        tips.SetToolTip(position, "Posizione del giocatore, non il compito tattico. Bravo: almeno 15/20; può giocare: almeno 12/20.");
        tips.SetToolTip(foot, "Dominante: più forte dell'altro piede. Entrambi forti: entrambi almeno 15/20.");
        foreach (var range in new[] { age, ca, pa, height, growth }) range.Changed += (_, _) => Notify();
        foreach (var combo in new[] { position, level, contract, foot, loan }) combo.SelectedIndexChanged += (_, _) => { level.Enabled = position.SelectedIndex > 0; Notify(); };
        SetLanguage(true);
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
        var old = Key(position);
        position.Items.Clear(); position.Items.Add(new Choice("", italian ? "Tutte le posizioni" : "Any position"));
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

