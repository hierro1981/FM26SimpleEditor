using System.ComponentModel;
using System.Globalization;

namespace FM26SimpleEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var setup = new RuntimeSetupForm();
        if (setup.ShowDialog() != DialogResult.OK || setup.PythonExecutable is null) return;
        Application.Run(new MainForm(setup.PythonExecutable));
    }
}

public sealed class MainForm : Form
{
    private readonly BackendClient backend;
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage searchTab = new("1  Ricerca giocatori");
    private readonly TabPage editTab = new("2  Modifica giocatore");
    private readonly TabPage previewTab = new("3  Anteprima nel gioco");
    private readonly TabPage comparisonTab = new("4  Confronto (0)");
    private readonly TabPage historyTab = new("5  Storico");
    private readonly ComparisonPanel comparison = new();
    private readonly HistoryPanel historyPanel = new();
    private readonly HashSet<uint> comparisonPool = [];
    private readonly ContextMenuStrip comparisonMenu = new();
    private readonly Button open = new() { Text = "Apri salvataggio", AutoSize = true };
    private readonly TextBox file = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly CheckBox italian = new() { Text = "Etichette in italiano", Checked = true, AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly TextBox filter = new() { Dock = DockStyle.Fill, PlaceholderText = "Cerca per nome, squadra, ID pubblico o ID interno" };
    private readonly ComboBox team = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox teamSearch = new() { Dock = DockStyle.Fill, PlaceholderText = "Filtra squadre, es. tri" };
    private readonly ComboBox loanTeam = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private static readonly Color LoanBack = Color.FromArgb(255, 247, 220);
    private static readonly Color LoanInk = Color.FromArgb(97, 68, 8);
    private static readonly Color LoanSelection = Color.FromArgb(143, 98, 20);
    private readonly SearchOptions options = new();
    private readonly Label searchInfo = new() { AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left };
    private readonly CheckBox showFilters = new() { AutoSize = true, Text = "Mostra filtri opzionali", Checked = true };
    private readonly Button clearFilters = new() { Text = "Azzera filtri", AutoSize = true };
    private readonly Button choose = new() { Text = "Apri scheda giocatore →", AutoSize = true, Enabled = false };
    private readonly Label results = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly DataGridView players = new();
    private readonly ComboBox relevancePosition = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    private readonly DataGridView attributes = new();
    private readonly DataGridView positions = new();
    private readonly NumericUpDown ca = new() { Minimum = 0, Maximum = 255, Width = 80 };
    private readonly NumericUpDown pa = new() { Minimum = -10, Maximum = 255, Width = 80 };
    private readonly ValueMeter caMeter = new() { Maximum = 200 };
    private readonly ValueMeter paMeter = new() { Maximum = 200 };
    private readonly DateTimePicker birth = new() { Format = DateTimePickerFormat.Short, MinDate = new DateTime(1850, 1, 1), Width = 145, Enabled = false };
    private readonly CheckBox changeBirth = new() { Text = "Modifica data di nascita", AutoSize = true, Enabled = false };
    private readonly Label identity = new() { AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
    private readonly Label affiliation = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label birthInfo = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(1150, 0) };
    private readonly Label changes = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Button save = new() { Text = "Salva modifiche", AutoSize = true, Enabled = false };
    private readonly Button reset = new() { Text = "Ripristina giocatore", AutoSize = true, Enabled = false };
    private readonly Button view = new() { Text = "Anteprima →", AutoSize = true, Enabled = false };
    private readonly Button compareButton = new() { Text = "Confronta…", AutoSize = true, Enabled = false };
    private readonly Button historyButton = new() { Text = "Storico…", AutoSize = true, Enabled = false };
    private readonly Label state = new() { AutoSize = true, Text = "Apri un salvataggio .fm per iniziare", Anchor = AnchorStyles.Left };
    private readonly Label progressText = new() { AutoSize = true, Anchor = AnchorStyles.Left, Visible = false };
    private readonly ProgressBar progress = new() { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25, Width = 125, Height = 18, Visible = false };
    private readonly PlayerPreview preview = new();
    private readonly Label emptyPreview = new() { Text = "Seleziona un giocatore nella scheda Ricerca.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Panel editing = new() { Dock = DockStyle.Fill, Enabled = false };
    private readonly ToolTip tips = new();
    private readonly System.Windows.Forms.Timer debounce = new() { Interval = 220 };
    private List<PlayerRow> catalog = [];
    private Dictionary<uint, PlayerRow> catalogByUid = new();
    private List<string> allTeams = [];
    private int searchVersion;
    private string sortField = "name";
    private bool sortDescending;
    private PlayerDetail? detail;
    private int[] originalAttributes = [];
    private int[] originalPositions = [];
    private DateTime? gameDate;
    private bool busy;
    private bool binding;
    private bool previewStale = true;

    public MainForm(string pythonExecutable)
    {
        backend = new BackendClient(pythonExecutable);
        Text = "FM26 Simple Editor · v12";
        Font = new Font("Segoe UI", 10);
        Width = 1360; Height = 900;
        MinimumSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Fixed command cells avoid FlowLayoutPanel measuring a narrow, wrapped toolbar.
        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 4, RowCount = 2, Margin = Padding.Empty, Padding = new Padding(0, 0, 0, 4) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize)); toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        toolbar.Controls.Add(open, 0, 0); toolbar.Controls.Add(save, 1, 0);
        toolbar.Controls.Add(changes, 2, 0); toolbar.Controls.Add(italian, 3, 0);
        toolbar.Controls.Add(file, 0, 1); toolbar.SetColumnSpan(file, 4);
        var status = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(5, 7, 5, 0) };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); status.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        status.Controls.Add(state, 0, 0); status.Controls.Add(progress, 1, 0); status.Controls.Add(progressText, 2, 0);
        tabs.TabPages.AddRange([searchTab, editTab, previewTab, comparisonTab, historyTab]);
        BuildSearch(); BuildEditor();
        comparisonTab.Controls.Add(comparison); historyTab.Controls.Add(historyPanel);
        comparison.RemoveRequested += uid => { comparisonPool.Remove(uid); UpdatePoolLabel(); RefreshComparison(); };
        comparison.ClearRequested += () => { comparisonPool.Clear(); UpdatePoolLabel(); RefreshComparison(); };
        previewTab.Controls.Add(preview); previewTab.Controls.Add(emptyPreview); preview.Visible = false;
        root.Controls.Add(toolbar, 0, 0); root.Controls.Add(tabs, 0, 1); root.Controls.Add(status, 0, 2);
        Controls.Add(root);
        open.Click += async (_, _) => await OpenFile();
        choose.Click += async (_, _) => await LoadSelected();
        players.CellDoubleClick += async (_, args) => { if (args.RowIndex >= 0) await LoadSelected(); };
        players.SelectionChanged += (_, _) => choose.Enabled = !busy && players.SelectedRows.Count == 1;
        filter.TextChanged += (_, _) => QueueSearch(); team.SelectedIndexChanged += (_, _) => QueueSearch();
        teamSearch.TextChanged += (_, _) => RebuildTeamChoices();
        loanTeam.SelectedIndexChanged += (_, _) => { if (!binding) ApplyLoanTeamView(); };
        options.Changed += (_, _) => QueueSearch();
        clearFilters.Click += (_, _) => ResetSearch();
        showFilters.CheckedChanged += (_, _) => options.Visible = showFilters.Checked;
        debounce.Tick += (_, _) => { debounce.Stop(); RefreshSearch(); };
        italian.CheckedChanged += (_, _) => { FinishEdits(); BindFields(); options.SetLanguage(italian.Checked); RebuildTeamChoices(); RefreshSearch(); previewStale = true; RefreshPreview(); RefreshComparison(); historyPanel.SetLanguage(italian.Checked); };
        ca.ValueChanged += (_, _) => Changed(); pa.ValueChanged += (_, _) => Changed(); birth.ValueChanged += (_, _) => Changed();
        changeBirth.CheckedChanged += (_, _) => { birth.Enabled = changeBirth.Checked && detail?.BirthEditable == true && !busy; Changed(); };
        save.Click += async (_, _) => await SaveFile();
        reset.Click += (_, _) => ResetEdits();
        view.Click += (_, _) => { if (FinishEdits()) tabs.SelectedTab = previewTab; };
        compareButton.Click += (_, _) => { if (FinishEdits()) tabs.SelectedTab = comparisonTab; };
        historyButton.Click += (_, _) => { if (FinishEdits()) tabs.SelectedTab = historyTab; };
        tabs.Selecting += (_, args) =>
        {
            if (!binding && !busy && (args.TabPage == previewTab || args.TabPage == comparisonTab || args.TabPage == historyTab) && !FinishEdits()) args.Cancel = true;
        };
        tabs.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedTab == previewTab) { FinishEdits(); RefreshPreview(); }
            if (tabs.SelectedTab == comparisonTab) { FinishEdits(); RefreshComparison(); }
            if (tabs.SelectedTab == historyTab) RefreshHistory();
        };
        tips.SetToolTip(italian, "Cambia le etichette degli attributi e delle posizioni; l'ordine alfabetico segue la lingua scelta.");
        tips.SetToolTip(ca, "Abilità attuale: scala nominale 0–200.");
        tips.SetToolTip(pa, "Abilità potenziale: scala nominale 0–200. I codici negativi del file vengono conservati.");
        tips.SetToolTip(save, "Scrive una nuova copia e la rende il file di lavoro per le modifiche successive.");
        tips.SetToolTip(teamSearch, "Cerca una parte del nome, anche in mezzo: tri mostra tutte le squadre che contengono tri. Poi puoi scegliere una squadra nell'elenco.");
        tips.SetToolTip(team, "Tutte le corrispondenze usa il testo a sinistra; scegli una squadra per limitare i giocatori a quella squadra esatta.");
        tips.SetToolTip(loanTeam, "Per i giocatori in prestito scegli la squadra usata nell’elenco, nella ricerca e nell’ordinamento. Anteprima e confronto restano riferiti alla squadra in cui giocano.");
        team.Items.Add("Tutte le squadre"); team.SelectedIndex = 0;
    }

    private void BuildSearch()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var search = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty, Padding = new Padding(0, 0, 0, 4) };
        search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        for (int row = 0; row < 3; row++) search.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        search.Controls.Add(filter, 0, 0); search.SetColumnSpan(filter, 2); search.Controls.Add(teamSearch, 0, 1); search.Controls.Add(team, 1, 1);
        loanTeam.Items.AddRange(["In cui gioca (predefinita)", "Proprietaria"]); loanTeam.SelectedIndex = 0;
        var loanOptions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        loanOptions.Controls.AddRange([
            new Label { Text = "Prestiti nella squadra:", AutoSize = true, Anchor = AnchorStyles.Left }, loanTeam,
            new Label { Text = "● In prestito", AutoSize = true, Anchor = AnchorStyles.Left, BackColor = LoanBack, ForeColor = LoanInk, Padding = new Padding(5, 3, 5, 3) }
        ]);
        search.Controls.Add(loanOptions, 0, 2); search.SetColumnSpan(loanOptions, 2);
        var filtersHeader = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        filtersHeader.Controls.AddRange([showFilters, clearFilters, searchInfo]);
        ConfigureGrid(players); players.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; players.ReadOnly = true; players.MultiSelect = false;
        players.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        players.Columns.Add(Column("Name", "Giocatore", 20)); players.Columns.Add(Column("SearchTeamDisplay", "Squadra", 19));
        players.Columns.Add(Column("MainPosition", "Ruolo principale", 10)); players.Columns.Add(Column("Age", "Età", 4)); players.Columns.Add(Column("PositionSummary", "Posizioni ≥12", 12));
        players.Columns.Add(Column("Ca", "CA", 4)); players.Columns.Add(Column("Pa", "PA", 4));
        players.Columns.Add(Column("ContractEndDisplay", "Scadenza", 9)); players.Columns.Add(Column("ContractLabel", "Contratto", 12));
        players.Columns.Add(Column("Id", "ID pubblico", 8)); players.Columns.Add(Column("Uid", "ID interno", 8));
        int[] widths = [200, 220, 150, 60, 230, 65, 65, 115, 155, 110, 110];
        for (int index = 0; index < players.Columns.Count; index++)
        {
            var column = players.Columns[index]; column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.Width = widths[index]; column.MinimumWidth = 50; column.SortMode = DataGridViewColumnSortMode.Programmatic;
            if (column.DataPropertyName is "Age" or "Ca" or "Pa" or "Id" or "Uid" or "ContractEndDisplay") column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        players.Columns[0].Frozen = false;
        players.ColumnHeaderMouseClick += (_, args) =>
        {
            if (busy || args.ColumnIndex < 0) return;
            var selected = SortKey(players.Columns[args.ColumnIndex].DataPropertyName);
            sortDescending = selected == sortField && !sortDescending; sortField = selected; RefreshSearch();
        };
        var addComparison = new ToolStripMenuItem("Aggiungi all'elenco di confronto");
        var compareNow = new ToolStripMenuItem("Confronta con il giocatore aperto");
        PlayerRow? contextPlayer = null;
        comparisonMenu.Items.AddRange([addComparison,compareNow]); players.ContextMenuStrip = comparisonMenu;
        players.CellMouseDown += (_, args) =>
        {
            if (args.Button != MouseButtons.Right || args.RowIndex < 0) return;
            players.ClearSelection(); players.Rows[args.RowIndex].Selected = true;
            if (args.ColumnIndex >= 0) players.CurrentCell = players.Rows[args.RowIndex].Cells[args.ColumnIndex];
        };
        comparisonMenu.Opening += (_, args) =>
        {
            var row = SelectedRow(); contextPlayer = row; args.Cancel = row is null || busy;
            addComparison.Enabled = row is not null && !comparisonPool.Contains(row.Uid);
            addComparison.Text = addComparison.Enabled ? "Aggiungi all'elenco di confronto" : "Già nell'elenco di confronto";
            compareNow.Enabled = row is not null && detail is not null && row.Uid != detail.Player.Uid;
        };
        addComparison.Click += (_, _) => { if (contextPlayer is PlayerRow row) AddComparison(row.Uid); };
        compareNow.Click += (_, _) =>
        {
            if (contextPlayer is not PlayerRow row || detail is null || !FinishEdits()) return;
            AddComparison(row.Uid); tabs.SelectedTab = comparisonTab; comparison.PreferReference(row.Uid);
        };
        players.CellFormatting += (_, args) =>
        {
            if (args.RowIndex < 0 || args.ColumnIndex < 0 || players.Rows[args.RowIndex].DataBoundItem is not PlayerRow row) return;
            bool onLoan = row.OnLoan is true;
            args.CellStyle!.BackColor = onLoan ? LoanBack : SystemColors.Window;
            args.CellStyle.ForeColor = onLoan ? LoanInk : SystemColors.WindowText;
            args.CellStyle.SelectionBackColor = onLoan ? LoanSelection : SystemColors.Highlight;
            args.CellStyle.SelectionForeColor = onLoan ? Color.White : SystemColors.HighlightText;
            players.Rows[args.RowIndex].Cells[args.ColumnIndex].ToolTipText = row.LoanDescription;
            if (args.RowIndex >= 0 && args.ColumnIndex >= 0 && players.Columns[args.ColumnIndex].DataPropertyName == "MainPosition" && players.Rows[args.RowIndex].DataBoundItem is PlayerRow mainRow)
            {
                args.Value = PositionScheme.Compact(mainRow.Positions, italian.Checked);
                players.Rows[args.RowIndex].Cells[args.ColumnIndex].ToolTipText = string.Join("; ", mainRow.Positions.Where(p => p.Value > 1).OrderBy(p => PositionScheme.Rank(p.Key)).Select(p => $"{Labels.Position(p.Key, italian.Checked)}: {p.Value}/20")) + (mainRow.OnLoan is true ? "\n" + mainRow.LoanDescription : "");
            }
            if (args.RowIndex >= 0 && players.Columns[args.ColumnIndex].DataPropertyName == "PositionSummary" && players.Rows[args.RowIndex].DataBoundItem is PlayerRow player)
                args.Value = string.Join(", ", player.Positions.Where(pair => pair.Value >= 12).OrderByDescending(pair => pair.Value).Select(pair => $"{Labels.PositionCode(pair.Key, italian.Checked)} ({pair.Value})"));
        };
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 9, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(results, 0, 0); footer.Controls.Add(choose, 1, 0);
        root.RowStyles[2].SizeType = SizeType.Absolute;
        void FitFilters()
        {
            int width = root.ClientSize.Width - root.Padding.Horizontal - options.Margin.Horizontal;
            int height = showFilters.Checked ? options.HeightForWidth(width) : 0;
            if (Math.Abs(root.RowStyles[2].Height - height) > .5f) root.RowStyles[2].Height = height;
        }
        root.Layout += (_, _) => FitFilters();
        showFilters.CheckedChanged += (_, _) => FitFilters();
        root.Controls.Add(search, 0, 0); root.Controls.Add(filtersHeader, 0, 1); root.Controls.Add(options, 0, 2); root.Controls.Add(players, 0, 3); root.Controls.Add(footer, 0, 4);
        searchTab.Controls.Add(root);
    }

    private void BuildEditor()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var header = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 0, 0, 8) };
        identity.Text = "Nessun giocatore selezionato"; affiliation.Text = "Apri una scheda dalla Ricerca.";
        header.Controls.Add(identity); header.Controls.Add(affiliation);
        var properties = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
        properties.Controls.AddRange([new Label { Text = "CA", AutoSize = true, Margin = new Padding(3, 17, 5, 0) }, ValueMeter.Wrap(ca,caMeter), new Label { Text = "PA", AutoSize = true, Margin = new Padding(14, 17, 5, 0) }, ValueMeter.Wrap(pa,paMeter), changeBirth, birth]);
        changeBirth.Margin = new Padding(24, 7, 5, 0);
        var split = new SplitContainer { Size = new Size(1100, 500), Dock = DockStyle.Fill, SplitterDistance = 700 };
        var attrGroup = new GroupBox { Text = "Attributi grezzi · scala nominale 1–100", Dock = DockStyle.Fill, Padding = new Padding(10) };
        var posGroup = new GroupBox { Text = "Familiarità nelle posizioni · 1–20", Dock = DockStyle.Fill, Padding = new Padding(10) };
        ConfigureEditGrid(attributes, 125); ConfigureEditGrid(positions, 20);
        var attrLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        attrLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); attrLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); attrLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var roleLine = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        roleLine.Controls.Add(new Label { Text = "Rilevanza per posizione", AutoSize = true }); roleLine.Controls.Add(relevancePosition);
        var keyLine = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        foreach (int tier in new[] { 1, 2, 3 }) keyLine.Controls.Add(new Label { Text = RoleImportance.Name(tier, true), AutoSize = true, ForeColor = RoleImportance.Ink(tier), BackColor = RoleImportance.Back(tier) });
        keyLine.Controls.Add(new Label { Text = "Guida editor, non pesi ufficiali FM", AutoSize = true });
        attrLayout.Controls.Add(roleLine, 0, 0); attrLayout.Controls.Add(keyLine, 0, 1); attrLayout.Controls.Add(attributes, 0, 2);
        attrGroup.Controls.Add(attrLayout); posGroup.Controls.Add(positions);
        relevancePosition.SelectedIndexChanged += (_, _) => attributes.Invalidate();
        attributes.CellFormatting += (_, args) =>
        {
            if (args.RowIndex < 0 || args.ColumnIndex != 0 || detail is null || attributes.Rows[args.RowIndex].DataBoundItem is not EditableField field) return;
            string position = relevancePosition.SelectedIndex > 0 ? PositionScheme.Order[relevancePosition.SelectedIndex - 1] : PositionScheme.Main(detail.Positions);
            int tier = RoleImportance.Tier(position, field.Key);
            args.CellStyle!.ForeColor = RoleImportance.Ink(tier); args.CellStyle.BackColor = RoleImportance.Back(tier);
            attributes.Rows[args.RowIndex].Cells[0].ToolTipText = RoleImportance.Name(tier, italian.Checked) + " · " + Labels.Position(position, italian.Checked);
        };
        split.Panel1.Controls.Add(attrGroup); split.Panel2.Controls.Add(posGroup);
        var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(0, 10, 0, 0) };
        footer.Controls.AddRange([reset, view, compareButton, historyButton]);
        root.Controls.Add(header, 0, 0); root.Controls.Add(properties, 0, 1); root.Controls.Add(birthInfo, 0, 2); root.Controls.Add(split, 0, 3); root.Controls.Add(footer, 0, 4);
        editing.Controls.Add(root); editTab.Controls.Add(editing);
    }

    private static DataGridViewTextBoxColumn Column(string property, string header, float weight) => new()
    {
        DataPropertyName = property, HeaderText = header, FillWeight = weight,
        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable,
        DefaultCellStyle = new DataGridViewCellStyle { NullValue = "—" }
    };

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill; grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false; grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.FixedSingle; grid.RowTemplate.Height = 29;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ScrollBars = ScrollBars.Both; grid.AllowUserToResizeColumns = true;
    }

    private void ConfigureEditGrid(DataGridView grid, int maximum)
    {
        ConfigureGrid(grid);
        var label = Column("Label", "Attributo / posizione", 75); label.ReadOnly = true;
        var value = Column("Value", "Valore", 25); value.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        value.CellTemplate = new ValueBarCell(maximum == 125 ? 100 : 20); grid.RowTemplate.Height = 41;
        grid.Columns.Add(label); grid.Columns.Add(value);
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.CellValidating += (_, args) =>
        {
            if (binding || args.ColumnIndex != 1 || args.RowIndex < 0) return;
            if (!int.TryParse(Convert.ToString(args.FormattedValue, CultureInfo.InvariantCulture), out var number) || number < 1 || number > maximum)
            {
                args.Cancel = true; grid.Rows[args.RowIndex].ErrorText = $"Inserisci un intero da 1 a {maximum}.";
                state.Text = grid.Rows[args.RowIndex].ErrorText;
            }
            else grid.Rows[args.RowIndex].ErrorText = "";
        };
        grid.CellValueChanged += (_, _) => Changed();
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (!binding) Changed();
        };
        grid.DataError += (_, args) => { args.ThrowException = false; args.Cancel = true; state.Text = "Inserisci un valore numerico valido."; };
    }

    private void QueueSearch() { if (binding) return; debounce.Stop(); debounce.Start(); }

    private async void RefreshSearch()
    {
        if (binding || busy) return;
        int version = ++searchVersion;
        string? selectedTeam = team.SelectedIndex > 0 ? team.SelectedItem?.ToString() : null;
        int count = options.ActiveCount + (selectedTeam is not null || teamSearch.Text.Trim().Length > 0 ? 1 : 0) + (filter.Text.Trim().Length > 0 ? 1 : 0);
        searchInfo.Text = $"{count} filtri attivi · data partita {gameDate?.ToString("dd/MM/yyyy") ?? "non disponibile"}";
        if (catalog.Count == 0) { players.DataSource = null; results.Text = "Apri un salvataggio per cercare."; return; }
        try
        {
            var query = options.Request(filter.Text.Trim(), selectedTeam, teamSearch.Text.Trim()); query.Sort = sortField; query.Descending = sortDescending;
            query.LoanTeam = loanTeam.SelectedIndex == 1 ? "owner" : "playing";
            var result = await backend.Send<SearchResult>(query);
            if (version != searchVersion || busy || IsDisposed) return;
            var rows = result.Uids.Where(uid => catalogByUid.ContainsKey(uid)).Select(uid => catalogByUid[uid]).ToList();
            players.DataSource = new BindingList<PlayerRow>(rows); players.ClearSelection();
            foreach (DataGridViewColumn column in players.Columns) column.HeaderCell.SortGlyphDirection = SortKey(column.DataPropertyName) == sortField ? sortDescending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
            results.Text = $"{result.Total:N0} risultati su {catalog.Count:N0} giocatori" + (result.Total > 2500 ? " · mostrati i primi 2.500" : "");
        }
        catch (Exception exc) { if (version == searchVersion && !IsDisposed) state.Text = "Ricerca non riuscita: " + exc.Message; }
    }

    private void ApplyLoanTeamView()
    {
        foreach (var player in catalog) player.ShowLoanOwner = loanTeam.SelectedIndex == 1;
        if (detail is not null) detail.Player.ShowLoanOwner = loanTeam.SelectedIndex == 1;
        allTeams = catalog.Select(player => player.SearchTeam).Where(name => name.Length > 0).Distinct().ToList();
        RebuildTeamChoices();
    }

    private void RebuildTeamChoices()
    {
        if (binding) return;
        var selected = team.SelectedIndex > 0 ? team.SelectedItem?.ToString() : null;
        var text = teamSearch.Text.Trim();
        binding = true; team.BeginUpdate(); team.Items.Clear();
        team.Items.Add(text.Length > 0 ? "Tutte le corrispondenze" : "Tutte le squadre");
        team.Items.AddRange(allTeams.Where(name => text.Length == 0 || name.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderBy(name => name, Labels.Comparer(italian.Checked)).Cast<object>().ToArray());
        int selectedIndex = selected is null ? -1 : team.Items.IndexOf(selected);
        team.SelectedIndex = selectedIndex > 0 ? selectedIndex : 0;
        team.EndUpdate(); binding = false; QueueSearch();
    }

    private void ResetSearch()
    {
        binding = true; filter.Clear(); teamSearch.Clear(); options.ResetFilters(); team.SelectedIndex = 0; binding = false;
        RebuildTeamChoices(); QueueSearch();
    }

    private static string SortKey(string property) => property switch
    {
        "Name" => "name", "Team" or "SearchTeamDisplay" => "team", "Age" => "age", "Ca" => "ca", "Pa" => "pa",
        "MainPosition" => "mainPosition", "Id" => "id", "Uid" => "uid", "ContractEndDisplay" => "contractEnd", "ContractLabel" => "contractStatus", "PositionSummary" => "positions",
        _ => "name"
    };

    private PlayerRow? SelectedRow() => players.SelectedRows.Count == 1 ? players.SelectedRows[0].DataBoundItem as PlayerRow : null;
    private void UpdatePoolLabel() => comparisonTab.Text = $"4  Confronto ({comparisonPool.Count})";
    private void AddComparison(uint uid)
    {
        if (!catalogByUid.ContainsKey(uid)) return;
        comparisonPool.Add(uid); UpdatePoolLabel();
        if (!binding && !busy) RefreshComparison();
    }

    private void RefreshComparison()
    {
        if (busy || binding || tabs.SelectedTab != comparisonTab) return;
        comparison.SetContext(detail,(int)ca.Value,(int)pa.Value,HasChanges(),catalog,comparisonPool,italian.Checked,
            uid => backend.Send<PlayerDetail>(new { command = "inspect", uid }));
    }

    private void RefreshHistory()
    {
        if (busy || tabs.SelectedTab != historyTab) return;
        historyPanel.SetContext(detail,italian.Checked,
            (uid,sources) => backend.Send<HistoryResult>(new { command = "history", uid, sources }));
    }

    private async Task OpenFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Salvataggi Football Manager (*.fm)|*.fm" };
        if (dialog.ShowDialog() != DialogResult.OK || !ConfirmDiscard()) return;
        SetBusy(true, "Lettura del salvataggio e del catalogo giocatori…", determinate: true);
        try
        {
            var result = await backend.Send<OpenResult>(new { command = "open", source = dialog.FileName }, ReportProgress);
            ReportProgress(new BackendProgress { Percent = 98, Phase = "Preparazione dell’elenco e dei filtri" });
            await Task.Yield();
            catalog = result.Players; catalogByUid = catalog.ToDictionary(player => player.Uid); file.Text = result.Source;
            gameDate = DateTime.TryParseExact(result.GameDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock) ? clock : null;
            detail = null; originalAttributes = []; originalPositions = [];
            comparisonPool.Clear(); UpdatePoolLabel(); comparison.ClearCache(); historyPanel.ClearSources();
            identity.Text = "Nessun giocatore selezionato"; affiliation.Text = "Apri una scheda dalla Ricerca.";
            editing.Enabled = false; preview.Visible = false; emptyPreview.Visible = true;
            binding = true; filter.Clear(); teamSearch.Clear(); options.ResetFilters(); team.SelectedIndex = 0; binding = false;
            ApplyLoanTeamView();
            tabs.SelectedTab = searchTab;
            progress.Value = 100; progressText.Text = "100%";
            state.Text = $"Salvataggio aperto · {catalog.Count:N0} giocatori";
        }
        catch (Exception exc) { Error("Errore apertura", exc); }
        finally { SetBusy(false); RefreshSearch(); Changed(); }
    }

    private async Task LoadSelected()
    {
        if (busy || players.SelectedRows.Count != 1 || players.SelectedRows[0].DataBoundItem is not PlayerRow row) return;
        if (detail?.Player.Uid == row.Uid) { tabs.SelectedTab = editTab; return; }
        if (!ConfirmDiscard()) return;
        SetBusy(true, $"Lettura di {row.Name}…");
        try
        {
            var selected = await backend.Send<PlayerDetail>(new { command = "inspect", uid = row.Uid });
            SetDetail(selected); tabs.SelectedTab = editTab;
            state.Text = $"{row.Name} · valori del salvataggio aperto";
        }
        catch (Exception exc) { Error("Errore lettura giocatore", exc); }
        finally { SetBusy(false); Changed(); }
    }

    private void SetDetail(PlayerDetail selected)
    {
        binding = true;
        selected.Player.ShowLoanOwner = loanTeam.SelectedIndex == 1;
        detail = selected; originalAttributes = PreviewMath.StoredOrder(selected.Attributes); originalPositions = PreviewMath.StoredOrder(selected.Positions);
        AddComparison(selected.Player.Uid);
        identity.Text = selected.Player.Name;
        affiliation.Text = $"{selected.Player.Team}  •  ID pubblico {selected.Player.Id?.ToString() ?? "—"}  •  ID interno {selected.Player.Uid}";
        ca.Value = Math.Clamp(selected.Player.Ca, 0, 255); pa.Value = Math.Clamp(selected.RawPa, -10, 255);
        birth.MaxDate = gameDate.HasValue && gameDate.Value.Year >= 1850 ? gameDate.Value : DateTimePicker.MaximumDateTime;
        if (DateTime.TryParseExact(selected.Player.Birth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthday) && birthday >= birth.MinDate && birthday <= birth.MaxDate) birth.Value = birthday;
        else birth.Value = birth.MinDate;
        changeBirth.Checked = false; changeBirth.Enabled = selected.BirthEditable; birth.Enabled = false;
        birthInfo.Text = selected.BirthEditable ? "La data viene modificata soltanto se attivi la casella. L'anteprima mostra i valori correnti senza scrivere sul file." : selected.BirthWarning;
        foreach (var field in selected.Attributes.Concat(selected.Positions)) field.PropertyChanged += FieldChanged;
        BindFields(); binding = false;
        editing.Enabled = true; view.Enabled = true; emptyPreview.Visible = false; previewStale = true;
    }

    private void FieldChanged(object? sender, PropertyChangedEventArgs args) { if (args.PropertyName == nameof(EditableField.Value)) Changed(); }

    private void BindFields()
    {
        if (detail is null) return;
        bool before = binding; binding = true;
        int roleIndex = Math.Max(0, relevancePosition.SelectedIndex);
        relevancePosition.Items.Clear(); relevancePosition.Items.Add(italian.Checked ? "Automatico · ruolo principale" : "Automatic · main position");
        relevancePosition.Items.AddRange(PositionScheme.Order.Select(key => (object)Labels.Position(key, italian.Checked)).ToArray()); relevancePosition.SelectedIndex = roleIndex;
        foreach (var field in detail.Attributes) field.Label = Labels.Attribute(field.Key, italian.Checked);
        foreach (var field in detail.Positions) field.Label = Labels.Position(field.Key, italian.Checked);
        attributes.DataSource = new BindingList<EditableField>(Labels.Alphabetical(detail.Attributes, italian.Checked).ToList());
        positions.DataSource = new BindingList<EditableField>(detail.Positions.OrderBy(field => PositionScheme.Rank(field.Key)).ToList());
        attributes.Columns[0].HeaderText = italian.Checked ? "Attributo" : "Attribute"; positions.Columns[0].HeaderText = italian.Checked ? "Posizione" : "Position";
        attributes.Columns[1].HeaderText = positions.Columns[1].HeaderText = italian.Checked ? "Valore grezzo" : "Raw value";
        positions.Columns[1].HeaderText = italian.Checked ? "Familiarità" : "Familiarity";
        binding = before;
    }

    private bool HasChanges()
    {
        if (detail is null) return false;
        return (int)ca.Value != detail.Player.Ca || (int)pa.Value != detail.RawPa ||
            !PreviewMath.StoredOrder(detail.Attributes).SequenceEqual(originalAttributes) || !PreviewMath.StoredOrder(detail.Positions).SequenceEqual(originalPositions) ||
            (changeBirth.Checked && birth.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != detail.Player.Birth);
    }

    private void Changed()
    {
        if (binding) return;
        bool dirty = HasChanges() || attributes.IsCurrentCellDirty || positions.IsCurrentCellDirty;
        save.Enabled = reset.Enabled = !busy && dirty;
        view.Enabled = !busy && detail is not null;
        compareButton.Enabled = historyButton.Enabled = !busy && detail is not null;
        caMeter.Value = (int)ca.Value; paMeter.Value = pa.Value >= 0 ? (int)pa.Value : null;
        changes.Text = file.Text.Length == 0 ? "" : dirty ? "● Modifiche non salvate" : "Nessuna modifica da salvare";
        changes.ForeColor = dirty ? Color.FromArgb(161, 99, 16) : Color.DimGray;
        Text = "FM26 Simple Editor · v12" + (dirty ? " *" : "");
        attributes.Invalidate(); previewStale = true; RefreshPreview(); RefreshComparison();
    }

    private bool FinishEdits() => attributes.EndEdit() && positions.EndEdit() && ValidateChildren();

    private bool ConfirmDiscard()
    {
        if (!FinishEdits()) return false;
        return !HasChanges() || MessageBox.Show("Scartare le modifiche non salvate al giocatore?", "Modifiche in corso", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private void ResetEdits()
    {
        if (detail is null) return;
        attributes.CancelEdit(); positions.CancelEdit(); binding = true;
        foreach (var field in detail.Attributes) field.Value = originalAttributes[field.Index];
        foreach (var field in detail.Positions) field.Value = originalPositions[field.Index];
        ca.Value = detail.Player.Ca; pa.Value = detail.RawPa; changeBirth.Checked = false;
        if (DateTime.TryParseExact(detail.Player.Birth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthday)) birth.Value = birthday;
        BindFields(); binding = false; Changed();
    }

    private void RefreshPreview()
    {
        if (!previewStale || detail is null || tabs.SelectedTab != previewTab) return;
        var player = detail.Player;
        var cohort = catalog.Where(other => other.Ca > 0 && (player.TeamId.HasValue ? other.TeamId == player.TeamId : player.ClubUid.HasValue && other.ClubUid == player.ClubUid)).ToList();
        double? mean = cohort.Count >= 3 ? cohort.Average(other => other.Ca) : null;
        DateTime? birthday = changeBirth.Checked ? birth.Value.Date : DateTime.TryParseExact(player.Birth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var original) ? original : null;
        preview.ShowPlayer(detail, (int)ca.Value, (int)pa.Value, birthday, italian.Checked, mean, cohort.Count);
        emptyPreview.Visible = false; preview.Visible = true; preview.BringToFront(); previewStale = false;
    }

    private async Task SaveFile()
    {
        if (busy || detail is null || !FinishEdits() || !HasChanges()) return;
        using var dialog = new SaveFileDialog { Filter = "Salvataggi Football Manager (*.fm)|*.fm", FileName = Path.GetFileNameWithoutExtension(file.Text) + "_modificato.fm", InitialDirectory = Path.GetDirectoryName(file.Text), OverwritePrompt = true };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        SetBusy(true, "Scrittura e verifica completa del salvataggio…", determinate: true);
        try
        {
            var result = await backend.Send<SaveResult>(new
            {
                command = "save", uid = detail.Player.Uid, id = detail.Player.Id,
                ca = (int)ca.Value, pa = (int)pa.Value, changeBirth = changeBirth.Checked,
                birth = birth.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                attributes = PreviewMath.StoredOrder(detail.Attributes), positions = PreviewMath.StoredOrder(detail.Positions), output = dialog.FileName
            }, ReportProgress);
            file.Text = result.Source;
            result.Detail.Player.ShowLoanOwner = loanTeam.SelectedIndex == 1;
            int at = catalog.FindIndex(player => player.Uid == result.Detail.Player.Uid);
            if (at >= 0) catalog[at] = result.Detail.Player;
            catalogByUid[result.Detail.Player.Uid] = result.Detail.Player;
            comparison.ClearCache();
            SetDetail(result.Detail);
            progress.Value = 100; progressText.Text = "100%";
            state.Text = "Copia salvata e verificata. Le prossime modifiche partiranno da questo file.";
            MessageBox.Show($"Copia salvata e verificata:\n{result.Source}", "Salvataggio completato", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exc) { Error("Salvataggio non riuscito", exc); }
        finally { SetBusy(false); RefreshSearch(); Changed(); }
    }

    private void ReportProgress(BackendProgress update)
    {
        if (IsDisposed || !busy) return;
        if (InvokeRequired) { BeginInvoke(new Action(() => ReportProgress(update))); return; }
        progress.Style = ProgressBarStyle.Continuous;
        progress.Value = Math.Max(progress.Value,Math.Clamp(update.Percent,0,98));
        progressText.Visible = true; progressText.Text = progress.Value + "%";
        string counter = "";
        if (update.Done.HasValue && update.Total is > 0)
            counter = update.Unit == "byte" ? $" · {update.Done.Value/1048576.0:0.0}/{update.Total.Value/1048576.0:0.0} MiB" : $" · {update.Done:N0}/{update.Total:N0} {update.Unit}";
        state.Text = update.Phase + counter;
    }

    private void SetBusy(bool value, string? message = null, bool determinate = false)
    {
        busy = value; UseWaitCursor = value; progress.Visible = value;
        progressText.Visible = value && determinate;
        if (value) { progress.Value = 0; progressText.Text = "0%"; progress.Style = determinate ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee; }
        if (value) ++searchVersion;
        open.Enabled = italian.Enabled = tabs.Enabled = !value;
        if (message is not null) state.Text = message;
        choose.Enabled = !value && players.SelectedRows.Count == 1;
        if (detail is not null) { editing.Enabled = !value; changeBirth.Enabled = !value && detail.BirthEditable; birth.Enabled = !value && changeBirth.Checked && detail.BirthEditable; }
        Changed();
    }

    private void Error(string title, Exception error)
    {
        state.Text = title + ": " + error.Message;
        MessageBox.Show(error.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void OnFormClosing(FormClosingEventArgs args)
    {
        if (busy) { args.Cancel = true; state.Text = "Attendi il completamento dell'operazione in corso."; return; }
        if (!ConfirmDiscard()) { args.Cancel = true; return; }
        base.OnFormClosing(args);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { debounce.Dispose(); tips.Dispose(); comparisonMenu.Dispose(); backend.Dispose(); }
        base.Dispose(disposing);
    }
}



