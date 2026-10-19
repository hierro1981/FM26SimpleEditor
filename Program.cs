using System.ComponentModel;
using System.Globalization;

namespace FM26SimpleEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        L10n.SetLanguage(AppSettings.Load(AppContext.BaseDirectory).Language);
        using var setup = new RuntimeSetupForm();
        if (setup.ShowDialog() != DialogResult.OK || setup.PythonExecutable is null) return;
        Application.Run(new MainForm(setup.PythonExecutable));
    }
}

public sealed class MainForm : Form
{
    private readonly BackendClient backend;
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage searchTab = new(L10n.Get("Text_632c1126bbb8"));
    private readonly TabPage editTab = new(L10n.Get("Text_d10c243588b5"));
    private readonly TabPage previewTab = new(L10n.Get("Text_e17d380480a5"));
    private readonly TabPage comparisonTab = new(L10n.Get("Text_49be2b3731fc"));
    private readonly TabPage historyTab = new(L10n.Get("Text_53a436ea8c92"));
    private readonly ComparisonPanel comparison = new();
    private readonly HistoryPanel historyPanel = new();
    private readonly HashSet<uint> comparisonPool = [];
    private readonly ContextMenuStrip comparisonMenu = new();
    private readonly Button open = new() { Text = L10n.Get("Text_96b6ef93f726"), AutoSize = true };
    private readonly TextBox file = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private sealed record LanguageChoice(string Id, string Label) { public override string ToString() => Label; }
    private readonly ComboBox language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125, Name = "LanguageSelector", AccessibleName = L10n.Get("LanguageLabel") };
    private readonly Button about = new() { Text = L10n.Get("AboutButton"), AutoSize = true };
    private readonly string configurationDirectory;
    private bool changingLanguage;
    private bool UseItalian => L10n.IsItalian;
    private readonly TextBox filter = new() { Dock = DockStyle.Fill, PlaceholderText = L10n.Get("Text_4f6eac65b3be") };
    private readonly ComboBox team = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox teamSearch = new() { Dock = DockStyle.Fill, PlaceholderText = L10n.Get("Text_751a743435ea") };
    private readonly ComboBox loanTeam = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private static readonly Color LoanBack = Color.FromArgb(255, 247, 220);
    private static readonly Color LoanInk = Color.FromArgb(97, 68, 8);
    private static readonly Color LoanSelection = Color.FromArgb(143, 98, 20);
    private readonly SearchOptions options = new();
    private readonly Label searchInfo = new() { AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left };
    private readonly CheckBox showFilters = new() { AutoSize = true, Text = L10n.Get("Text_0215c86d1b54"), Checked = true };
    private readonly Button clearFilters = new() { Text = L10n.Get("Text_c4483e052140"), AutoSize = true };
    private readonly Button choose = new() { Text = L10n.Get("Text_9337b648a0e1"), AutoSize = true, Enabled = false };
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
    private readonly CheckBox changeBirth = new() { Text = L10n.Get("Text_86941499fe9e"), AutoSize = true, Enabled = false };
    private readonly Label identity = new() { AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
    private readonly Label affiliation = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label publicIdText = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label internalIdText = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly CopyValueButton copyPlayerName = new() { Name = "CopyPlayerName", AccessibleName = L10n.Get("CopyPlayerName"), Enabled = false };
    private readonly CopyValueButton copyPublicId = new() { Name = "CopyPublicId", AccessibleName = L10n.Get("CopyPublicId"), Enabled = false };
    private readonly CopyValueButton copyInternalId = new() { Name = "CopyInternalId", AccessibleName = L10n.Get("CopyInternalId"), Enabled = false };
    private readonly Label birthInfo = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(1150, 0) };
    private readonly Label changes = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Button save = new() { Text = L10n.Get("Text_55b0f70b6858"), AutoSize = true, Enabled = false };
    private readonly Button reset = new() { Text = L10n.Get("Text_6263f825860b"), AutoSize = true, Enabled = false };
    private readonly Button view = new() { Text = L10n.Get("Text_52bfc8e906b8"), AutoSize = true, Enabled = false };
    private readonly Button compareButton = new() { Text = L10n.Get("Text_82801b689bdc"), AutoSize = true, Enabled = false };
    private readonly Button historyButton = new() { Text = L10n.Get("Text_4c9f2babc23a"), AutoSize = true, Enabled = false };
    private readonly Label state = new() { AutoSize = true, Text = L10n.Get("Text_c0c8df4a07ac"), Anchor = AnchorStyles.Left };
    private readonly Label progressText = new() { AutoSize = true, Anchor = AnchorStyles.Left, Visible = false };
    private readonly ProgressBar progress = new() { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 25, Width = 125, Height = 18, Visible = false };
    private readonly PlayerPreview preview = new();
    private readonly Label emptyPreview = new() { Text = L10n.Get("Text_a237f9809ea5"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
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

    public MainForm(string pythonExecutable, string? configurationDirectory = null)
    {
        this.configurationDirectory = configurationDirectory ?? AppContext.BaseDirectory;
        backend = new BackendClient(pythonExecutable);
        language.Items.AddRange([new LanguageChoice("it", "Italiano"), new LanguageChoice("en", "English")]);
        language.SelectedIndex = UseItalian ? 0 : 1;
        using var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("FM26SimpleEditor.App.ico");
        if (iconStream is not null) Icon = new Icon(iconStream);
        Text = AppInfo.Title;
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
        toolbar.Controls.Add(changes, 2, 0);
        var languageTools = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        languageTools.Controls.AddRange([new Label { Text = L10n.Get("LanguageLabel"), AutoSize = true, Anchor = AnchorStyles.Left }, language, about]);
        toolbar.Controls.Add(languageTools, 3, 0);
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
        language.SelectedIndexChanged += (_, _) => ChangeLanguage();
        about.Click += (_, _) => { using var dialog = new AboutForm(); dialog.ShowDialog(this); };
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
        copyPlayerName.Click += (_, _) => CopyIdentity(detail?.Player.Name);
        copyPublicId.Click += (_, _) => CopyIdentity(detail?.Player.Id?.ToString(CultureInfo.InvariantCulture));
        copyInternalId.Click += (_, _) => CopyIdentity(detail?.Player.Uid.ToString(CultureInfo.InvariantCulture));
        tips.SetToolTip(copyPlayerName, L10n.Get("CopyPlayerName"));
        tips.SetToolTip(copyPublicId, L10n.Get("CopyPublicId"));
        tips.SetToolTip(copyInternalId, L10n.Get("CopyInternalId"));
        tips.SetToolTip(language, L10n.Get("Text_c7ba90415dbf"));
        tips.SetToolTip(ca, L10n.Get("Text_a2e3e958ed35"));
        tips.SetToolTip(pa, L10n.Get("Text_064e19de6695"));
        tips.SetToolTip(save, L10n.Get("Text_47d9a2b22da4"));
        tips.SetToolTip(teamSearch, L10n.Get("Text_01632e980f38"));
        tips.SetToolTip(team, L10n.Get("Text_1229c3bc79fc"));
        tips.SetToolTip(loanTeam, L10n.Get("Text_41992da10449"));
        team.Items.Add(L10n.Get("Text_452ac98b053e")); team.SelectedIndex = 0;
        options.SetLanguage(UseItalian);
        L10n.Capture(this, tips);
    }

    private void ChangeLanguage()
    {
        if (changingLanguage || language.SelectedItem is not LanguageChoice selected) return;
        changingLanguage = true;
        try
        {
            if (!FinishEdits()) { language.SelectedIndex = UseItalian ? 0 : 1; return; }
            L10n.SetLanguage(selected.Id);
            L10n.Apply(this);
            options.SetLanguage(UseItalian);
            state.Text = L10n.Translate(state.Text);
            UpdatePlayerIdentity();
            if (detail is not null) birthInfo.Text = detail.BirthEditable ? L10n.Get("Text_ec60709e94bb") : L10n.Backend(detail.BirthWarning);
            UpdatePoolLabel(); BindFields(); RebuildTeamChoices(); RefreshSearch();
            previewStale = true; RefreshPreview(); RefreshComparison(); historyPanel.SetLanguage(UseItalian);
            Changed();
            try { AppSettings.Save(configurationDirectory, selected.Id); }
            catch (Exception exc) when (exc is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, L10n.Format("ConfigSaveError", Path.Combine(configurationDirectory, AppSettings.FileName), exc.Message),
                    L10n.Get("ConfigWarning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally { changingLanguage = false; }
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
        loanTeam.Items.AddRange([L10n.Get("Text_1327967db178"), L10n.Get("Text_8b402958d93e")]); loanTeam.SelectedIndex = 0;
        var loanOptions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        loanOptions.Controls.AddRange([
            new Label { Text = L10n.Get("Text_4cab1256af2b"), AutoSize = true, Anchor = AnchorStyles.Left }, loanTeam,
            new Label { Text = L10n.Get("Text_fce03e78db6f"), AutoSize = true, Anchor = AnchorStyles.Left, BackColor = LoanBack, ForeColor = LoanInk, Padding = new Padding(5, 3, 5, 3) }
        ]);
        search.Controls.Add(loanOptions, 0, 2); search.SetColumnSpan(loanOptions, 2);
        var filtersHeader = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        filtersHeader.Controls.AddRange([showFilters, clearFilters, searchInfo]);
        ConfigureGrid(players); players.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; players.ReadOnly = true; players.MultiSelect = false;
        players.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        players.Columns.Add(Column("Name", L10n.Get("Text_f537c6bd038e"), 20)); players.Columns.Add(Column("SearchTeamDisplay", L10n.Get("Text_3b0a75ea292b"), 19));
        players.Columns.Add(Column("MainPosition", L10n.Get("Text_045cb480645d"), 10)); players.Columns.Add(Column("Age", L10n.Get("Text_9efdc62c88bf"), 4)); players.Columns.Add(Column("PositionSummary", L10n.Get("Text_5aa749965391"), 12));
        players.Columns.Add(Column("Ca", "CA", 4)); players.Columns.Add(Column("Pa", "PA", 4));
        players.Columns.Add(Column("ContractEndDisplay", L10n.Get("Text_51bf843f1751"), 9)); players.Columns.Add(Column("ContractLabel", L10n.Get("Text_725424c55624"), 12));
        players.Columns.Add(Column("Id", L10n.Get("Text_358796f71be1"), 8)); players.Columns.Add(Column("Uid", L10n.Get("Text_e1e8b54d45e2"), 8));
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
        var addComparison = new ToolStripMenuItem(L10n.Get("Text_885725b27cce"));
        var compareNow = new ToolStripMenuItem(L10n.Get("Text_f101d9a9a54f"));
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
            addComparison.Text = addComparison.Enabled ? L10n.Get("Text_885725b27cce") : L10n.Get("Text_8b48e2e8ec50");
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
                args.Value = PositionScheme.Compact(mainRow.Positions, UseItalian);
                players.Rows[args.RowIndex].Cells[args.ColumnIndex].ToolTipText = string.Join("; ", mainRow.Positions.Where(p => p.Value > 1).OrderBy(p => PositionScheme.Rank(p.Key)).Select(p => $"{Labels.Position(p.Key, UseItalian)}: {p.Value}/20")) + (mainRow.OnLoan is true ? "\n" + mainRow.LoanDescription : "");
            }
            if (args.RowIndex >= 0 && players.Columns[args.ColumnIndex].DataPropertyName == "PositionSummary" && players.Rows[args.RowIndex].DataBoundItem is PlayerRow player)
                args.Value = string.Join(", ", player.Positions.Where(pair => pair.Value >= 12).OrderByDescending(pair => pair.Value).Select(pair => $"{Labels.PositionCode(pair.Key, UseItalian)} ({pair.Value})"));
        };
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 9, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(results, 0, 0); footer.Controls.Add(choose, 1, 0);
        root.RowStyles[2].SizeType = SizeType.Absolute;
        void FitFilters()
        {
            int width = root.ClientSize.Width - root.Padding.Horizontal - options.Margin.Horizontal;
            int height = showFilters.Checked ? options.HeightForWidth(width) + options.Margin.Vertical : 0;
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
        identity.Text = L10n.Get("Text_3f8498e92e4b"); affiliation.Text = L10n.Get("Text_9a36d649406a");
        var nameRow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
        copyPlayerName.Anchor = AnchorStyles.Left;
        nameRow.Controls.AddRange([identity, copyPlayerName]);
        var identityRow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
        identityRow.Controls.AddRange([affiliation, publicIdText, copyPublicId, internalIdText, copyInternalId]);
        header.Controls.Add(nameRow); header.Controls.Add(identityRow);
        var properties = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };
        properties.Controls.AddRange([new Label { Text = "CA", AutoSize = true, Margin = new Padding(3, 17, 5, 0) }, ValueMeter.Wrap(ca,caMeter), new Label { Text = "PA", AutoSize = true, Margin = new Padding(14, 17, 5, 0) }, ValueMeter.Wrap(pa,paMeter), changeBirth, birth]);
        changeBirth.Margin = new Padding(24, 7, 5, 0);
        var split = new SplitContainer { Size = new Size(1100, 500), Dock = DockStyle.Fill, SplitterDistance = 700 };
        var attrGroup = new GroupBox { Text = L10n.Get("Text_96fd7b35ee9e"), Dock = DockStyle.Fill, Padding = new Padding(10) };
        var posGroup = new GroupBox { Text = L10n.Get("Text_1f6d6e1f48d8"), Dock = DockStyle.Fill, Padding = new Padding(10) };
        ConfigureEditGrid(attributes, 125); ConfigureEditGrid(positions, 20);
        var attrLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        attrLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); attrLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); attrLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var roleLine = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        roleLine.Controls.Add(new Label { Text = L10n.Get("Text_16a31a1428fa"), AutoSize = true }); roleLine.Controls.Add(relevancePosition);
        var keyLine = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        foreach (int tier in new[] { 1, 2, 3 }) keyLine.Controls.Add(new Label { Text = RoleImportance.Name(tier, true), AutoSize = true, ForeColor = RoleImportance.Ink(tier), BackColor = RoleImportance.Back(tier) });
        keyLine.Controls.Add(new Label { Text = L10n.Get("Text_474c06af6715"), AutoSize = true });
        attrLayout.Controls.Add(roleLine, 0, 0); attrLayout.Controls.Add(keyLine, 0, 1); attrLayout.Controls.Add(attributes, 0, 2);
        attrGroup.Controls.Add(attrLayout); posGroup.Controls.Add(positions);
        relevancePosition.SelectedIndexChanged += (_, _) => attributes.Invalidate();
        attributes.CellFormatting += (_, args) =>
        {
            if (args.RowIndex < 0 || args.ColumnIndex != 0 || detail is null || attributes.Rows[args.RowIndex].DataBoundItem is not EditableField field) return;
            string position = relevancePosition.SelectedIndex > 0 ? PositionScheme.Order[relevancePosition.SelectedIndex - 1] : PositionScheme.Main(detail.Positions);
            int tier = RoleImportance.Tier(position, field.Key);
            args.CellStyle!.ForeColor = RoleImportance.Ink(tier); args.CellStyle.BackColor = RoleImportance.Back(tier);
            attributes.Rows[args.RowIndex].Cells[0].ToolTipText = RoleImportance.Name(tier, UseItalian) + " · " + Labels.Position(position, UseItalian);
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
        var label = Column("Label", L10n.Get("Text_51df0484073a"), 75); label.ReadOnly = true;
        var value = Column("Value", L10n.Get("Text_3b50ed0e6ec2"), 25); value.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        value.CellTemplate = new ValueBarCell(maximum == 125 ? 100 : 20); grid.RowTemplate.Height = 41;
        grid.Columns.Add(label); grid.Columns.Add(value);
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.CellValidating += (_, args) =>
        {
            if (binding || args.ColumnIndex != 1 || args.RowIndex < 0) return;
            if (!int.TryParse(Convert.ToString(args.FormattedValue, CultureInfo.InvariantCulture), out var number) || number < 1 || number > maximum)
            {
                args.Cancel = true; grid.Rows[args.RowIndex].ErrorText = L10n.Format("Text_972393f31323", maximum);
                state.Text = grid.Rows[args.RowIndex].ErrorText;
            }
            else grid.Rows[args.RowIndex].ErrorText = "";
        };
        grid.CellValueChanged += (_, _) => Changed();
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (!binding) Changed();
        };
        grid.DataError += (_, args) => { args.ThrowException = false; args.Cancel = true; state.Text = L10n.Get("Text_558b8c27d299"); };
    }

    private void QueueSearch() { if (binding) return; debounce.Stop(); debounce.Start(); }

    private async void RefreshSearch()
    {
        if (binding || busy) return;
        int version = ++searchVersion;
        string? selectedTeam = team.SelectedIndex > 0 ? team.SelectedItem?.ToString() : null;
        int count = options.ActiveCount + (selectedTeam is not null || teamSearch.Text.Trim().Length > 0 ? 1 : 0) + (filter.Text.Trim().Length > 0 ? 1 : 0);
        searchInfo.Text = L10n.Format("Text_946327641b50", count, gameDate?.ToString("dd/MM/yyyy") ?? L10n.Get("Text_1d80f0682e0b"));
        if (catalog.Count == 0) { players.DataSource = null; results.Text = L10n.Get("Text_b4739e9bd7a1"); return; }
        try
        {
            var query = options.Request(filter.Text.Trim(), selectedTeam, teamSearch.Text.Trim()); query.Sort = sortField; query.Descending = sortDescending;
            query.LoanTeam = loanTeam.SelectedIndex == 1 ? "owner" : "playing";
            var result = await backend.Send<SearchResult>(query);
            if (version != searchVersion || busy || IsDisposed) return;
            var rows = result.Uids.Where(uid => catalogByUid.ContainsKey(uid)).Select(uid => catalogByUid[uid]).ToList();
            players.DataSource = new BindingList<PlayerRow>(rows); players.ClearSelection();
            foreach (DataGridViewColumn column in players.Columns) column.HeaderCell.SortGlyphDirection = SortKey(column.DataPropertyName) == sortField ? sortDescending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
            results.Text = L10n.Format("Text_658469a26ec0", result.Total, catalog.Count) + (result.Total > 2500 ? L10n.Get("Text_839423d2d1a6") : "");
        }
        catch (Exception exc) { if (version == searchVersion && !IsDisposed) state.Text = L10n.Get("Text_5df832e19d28") + exc.Message; }
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
        team.Items.Add(text.Length > 0 ? L10n.Get("Text_0fb09ab452aa") : L10n.Get("Text_452ac98b053e"));
        team.Items.AddRange(allTeams.Where(name => text.Length == 0 || name.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderBy(name => name, Labels.Comparer(UseItalian)).Cast<object>().ToArray());
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
    private void UpdatePoolLabel() => comparisonTab.Text = L10n.Format("Text_7a436139381b", comparisonPool.Count);
    private void AddComparison(uint uid)
    {
        if (!catalogByUid.ContainsKey(uid)) return;
        comparisonPool.Add(uid); UpdatePoolLabel();
        if (!binding && !busy) RefreshComparison();
    }

    private void RefreshComparison()
    {
        if (busy || binding || tabs.SelectedTab != comparisonTab) return;
        comparison.SetContext(detail,(int)ca.Value,(int)pa.Value,HasChanges(),catalog,comparisonPool,UseItalian,
            uid => backend.Send<PlayerDetail>(new { command = "inspect", uid }));
    }

    private void RefreshHistory()
    {
        if (busy || tabs.SelectedTab != historyTab) return;
        historyPanel.SetContext(detail,UseItalian,
            (uid,sources) => backend.Send<HistoryResult>(new { command = "history", uid, sources }));
    }

    private async Task OpenFile()
    {
        using var dialog = new OpenFileDialog { Filter = L10n.Get("Text_b67eeb7fa174") };
        if (dialog.ShowDialog() != DialogResult.OK || !ConfirmDiscard()) return;
        SetBusy(true, L10n.Get("Text_beb6c62f8917"), determinate: true);
        try
        {
            var result = await backend.Send<OpenResult>(new { command = "open", source = dialog.FileName }, ReportProgress);
            ReportProgress(new BackendProgress { Percent = 98, Phase = L10n.Get("Text_e2d47e04db5f") });
            await Task.Yield();
            catalog = result.Players; catalogByUid = catalog.ToDictionary(player => player.Uid); file.Text = result.Source;
            gameDate = DateTime.TryParseExact(result.GameDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clock) ? clock : null;
            detail = null; originalAttributes = []; originalPositions = [];
            comparisonPool.Clear(); UpdatePoolLabel(); comparison.ClearCache(); historyPanel.ClearSources();
            identity.Text = L10n.Get("Text_3f8498e92e4b"); affiliation.Text = L10n.Get("Text_9a36d649406a");
            UpdatePlayerIdentity();
            editing.Enabled = false; preview.Visible = false; emptyPreview.Visible = true;
            binding = true; filter.Clear(); teamSearch.Clear(); options.ResetFilters(); team.SelectedIndex = 0; binding = false;
            ApplyLoanTeamView();
            tabs.SelectedTab = searchTab;
            progress.Value = 100; progressText.Text = "100%";
            state.Text = L10n.Format("Text_76ed2e73cde9", catalog.Count);
        }
        catch (Exception exc) { Error(L10n.Get("Text_9c589f02f00a"), exc); }
        finally { SetBusy(false); RefreshSearch(); Changed(); }
    }

    private async Task LoadSelected()
    {
        if (busy || players.SelectedRows.Count != 1 || players.SelectedRows[0].DataBoundItem is not PlayerRow row) return;
        if (detail?.Player.Uid == row.Uid) { tabs.SelectedTab = editTab; return; }
        if (!ConfirmDiscard()) return;
        SetBusy(true, L10n.Format("Text_0af5b1bce786", row.Name), determinate: true);
        try
        {
            var selected = await backend.Send<PlayerDetail>(new { command = "inspect", uid = row.Uid }, ReportProgress);
            ReportProgress(new BackendProgress { Percent = 98, Phase = L10n.Get("PlayerRendering") });
            SetDetail(selected); tabs.SelectedTab = editTab;
            progress.Value = 100; progressText.Text = "100%";
            state.Text = L10n.Format("Text_8bef81b4f3c8", row.Name);
        }
        catch (Exception exc) { Error(L10n.Get("Text_fcb5504e6a1f"), exc); }
        finally { SetBusy(false); Changed(); }
    }

    private void SetDetail(PlayerDetail selected)
    {
        binding = true;
        selected.Player.ShowLoanOwner = loanTeam.SelectedIndex == 1;
        detail = selected; originalAttributes = PreviewMath.StoredOrder(selected.Attributes); originalPositions = PreviewMath.StoredOrder(selected.Positions);
        AddComparison(selected.Player.Uid);
        identity.Text = selected.Player.Name;
        UpdatePlayerIdentity();
        ca.Value = Math.Clamp(selected.Player.Ca, 0, 255); pa.Value = Math.Clamp(selected.RawPa, -10, 255);
        birth.MaxDate = gameDate.HasValue && gameDate.Value.Year >= 1850 ? gameDate.Value : DateTimePicker.MaximumDateTime;
        if (DateTime.TryParseExact(selected.Player.Birth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthday) && birthday >= birth.MinDate && birthday <= birth.MaxDate) birth.Value = birthday;
        else birth.Value = birth.MinDate;
        changeBirth.Checked = false; changeBirth.Enabled = selected.BirthEditable; birth.Enabled = false;
        birthInfo.Text = selected.BirthEditable ? L10n.Get("Text_ec60709e94bb") : L10n.Backend(selected.BirthWarning);
        foreach (var field in selected.Attributes.Concat(selected.Positions)) field.PropertyChanged += FieldChanged;
        BindFields(); binding = false;
        editing.Enabled = true; view.Enabled = true; emptyPreview.Visible = false; previewStale = true;
    }

    private void UpdatePlayerIdentity()
    {
        copyPlayerName.Enabled = !string.IsNullOrEmpty(detail?.Player.Name);
        copyPublicId.Enabled = detail?.Player.Id.HasValue == true;
        copyInternalId.Enabled = detail is not null;
        if (detail is null) { publicIdText.Text = internalIdText.Text = ""; return; }
        affiliation.Text = detail.Player.Team;
        publicIdText.Text = L10n.Format("PublicIdValue", detail.Player.Id?.ToString(CultureInfo.InvariantCulture) ?? "—");
        internalIdText.Text = L10n.Format("InternalIdValue", detail.Player.Uid.ToString(CultureInfo.InvariantCulture));
    }

    private void CopyIdentity(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        try
        {
            Clipboard.SetText(value, TextDataFormat.UnicodeText);
            state.Text = L10n.Get("CopiedToClipboard");
        }
        catch (Exception exc) when (exc is System.Runtime.InteropServices.ExternalException or System.Threading.ThreadStateException)
        {
            MessageBox.Show(this, L10n.Get("ClipboardUnavailable"), L10n.Get("ClipboardTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void FieldChanged(object? sender, PropertyChangedEventArgs args) { if (args.PropertyName == nameof(EditableField.Value)) Changed(); }

    private void BindFields()
    {
        if (detail is null) return;
        bool before = binding; binding = true;
        int roleIndex = Math.Max(0, relevancePosition.SelectedIndex);
        relevancePosition.Items.Clear(); relevancePosition.Items.Add(UseItalian ? L10n.Get("Text_6d7f90c02494", true) : L10n.Get("Text_6d7f90c02494", false));
        relevancePosition.Items.AddRange(PositionScheme.Order.Select(key => (object)Labels.Position(key, UseItalian)).ToArray()); relevancePosition.SelectedIndex = roleIndex;
        foreach (var field in detail.Attributes) field.Label = Labels.Attribute(field.Key, UseItalian);
        foreach (var field in detail.Positions) field.Label = Labels.Position(field.Key, UseItalian);
        attributes.DataSource = new BindingList<EditableField>(Labels.Alphabetical(detail.Attributes, UseItalian).ToList());
        positions.DataSource = new BindingList<EditableField>(detail.Positions.OrderBy(field => PositionScheme.Rank(field.Key)).ToList());
        attributes.Columns[0].HeaderText = UseItalian ? L10n.Get("Text_707eecb875fc", true) : L10n.Get("Text_707eecb875fc", false); positions.Columns[0].HeaderText = UseItalian ? L10n.Get("Text_d7effa1c65a8", true) : L10n.Get("Text_d7effa1c65a8", false);
        attributes.Columns[1].HeaderText = positions.Columns[1].HeaderText = UseItalian ? L10n.Get("Text_7b4e2d0fae1d", true) : L10n.Get("Text_7b4e2d0fae1d", false);
        positions.Columns[1].HeaderText = UseItalian ? L10n.Get("Text_d648bc617064", true) : L10n.Get("Text_d648bc617064", false);
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
        changes.Text = file.Text.Length == 0 ? "" : dirty ? L10n.Get("Text_5f4c1937efd4") : L10n.Get("Text_01be256931b8");
        changes.ForeColor = dirty ? Color.FromArgb(161, 99, 16) : Color.DimGray;
        Text = AppInfo.Title + (dirty ? " *" : "");
        attributes.Invalidate(); previewStale = true; RefreshPreview(); RefreshComparison();
    }

    private bool FinishEdits() => attributes.EndEdit() && positions.EndEdit() && ValidateChildren();

    private bool ConfirmDiscard()
    {
        if (!FinishEdits()) return false;
        return !HasChanges() || MessageBox.Show(L10n.Get("Text_8fdbfdcebc3a"), L10n.Get("Text_4dba6a2f1b9f"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
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
        preview.ShowPlayer(detail, (int)ca.Value, (int)pa.Value, birthday, UseItalian, mean, cohort.Count);
        emptyPreview.Visible = false; preview.Visible = true; preview.BringToFront(); previewStale = false;
    }

    private async Task SaveFile()
    {
        if (busy || detail is null || !FinishEdits() || !HasChanges()) return;
        using var dialog = new SaveFileDialog { Filter = L10n.Get("Text_b67eeb7fa174"), FileName = Path.GetFileNameWithoutExtension(file.Text) + L10n.Get("ModifiedSuffix"), InitialDirectory = Path.GetDirectoryName(file.Text), OverwritePrompt = true };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        SetBusy(true, L10n.Get("Text_fd2a169c6549"), determinate: true);
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
            state.Text = L10n.Get("Text_6aef1e9c089a");
            MessageBox.Show(L10n.Format("Text_a6cc4366541a", result.Source), L10n.Get("Text_514019445ab2"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exc) { Error(L10n.Get("Text_9b6ca71eb272"), exc); }
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
            counter = update.Unit == "byte" ? $" · {update.Done.Value/1048576.0:0.0}/{update.Total.Value/1048576.0:0.0} MiB" : $" · {update.Done:N0}/{update.Total:N0} {L10n.Backend(update.Unit)}";
        state.Text = L10n.Backend(update.Phase) + counter;
    }

    private void SetBusy(bool value, string? message = null, bool determinate = false)
    {
        busy = value; UseWaitCursor = value; progress.Visible = value;
        progressText.Visible = value && determinate;
        if (value) { progress.Value = 0; progressText.Text = "0%"; progress.Style = determinate ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee; }
        if (value) ++searchVersion;
        open.Enabled = language.Enabled = tabs.Enabled = !value;
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
        if (busy) { args.Cancel = true; state.Text = L10n.Get("Text_53e4d698ead1"); return; }
        if (!ConfirmDiscard()) { args.Cancel = true; return; }
        base.OnFormClosing(args);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { debounce.Dispose(); tips.Dispose(); comparisonMenu.Dispose(); backend.Dispose(); }
        base.Dispose(disposing);
    }
}




