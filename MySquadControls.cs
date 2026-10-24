using System.ComponentModel;
using System.Globalization;

namespace FM26SimpleEditor;

public sealed class MySquadPanel : UserControl
{
    private readonly ComboBox club = new() { Name = "ManagedClubChoice", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox scope = new() { Name = "MySquadScope", DropDownStyle = ComboBoxStyle.DropDownList, Width = 270 };
    private readonly CheckBox includeLoans = new() { Name = "MySquadIncludeLoans", Text = L10n.Get("IncludeOutgoingLoans"), AutoSize = true };
    private readonly TextBox search = new() { Name = "MySquadSearch", Dock = DockStyle.Fill, PlaceholderText = L10n.Get("MySquadSearch") };
    private readonly Label status = new() { Name = "MySquadStatus", AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label count = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly DataGridView roster = Grid("MySquadRoster");
    private readonly DataGridView ranking = Grid("MySquadRanking");
    private readonly PitchControl pitch = new() { SelectionEnabled = true, MultiSelectionEnabled = true, SelectedPosition = "MC", MinimumSize = new Size(230, 350) };
    private readonly ComboBox position = new() { Name = "MySquadPosition", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox secondPosition = new() { Name = "MySquadSecondPosition", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox role = new() { Name = "MySquadRole", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, DropDownWidth = 480 };
    private readonly ComboBox secondRole = new() { Name = "MySquadSecondRole", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, DropDownWidth = 480 };
    private readonly ToolTip roleTips = new() { AutoPopDelay = 20000 };
    private string? selectedRoleId;
    private string? secondaryRoleId;
    private readonly Label advice = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Button open = new() { Text = L10n.Get("Text_9337b648a0e1"), AutoSize = true };
    private readonly Button openRanked = new() { Text = L10n.Get("Text_9337b648a0e1"), AutoSize = true };
    private readonly StaffPanel staff = new();
    private readonly StaffRecruitmentPanel recruitment = new();
    private readonly StaffPlanningPanel planning = new();
    private readonly TabPage planningTab = new(L10n.Get("PlanTab"));
    private readonly TabPage recruitmentTab = new(L10n.Get("RecruitTab"));
    private readonly TacticsPanel tactics = new();
    private readonly TabPage tacticsTab = new(L10n.Get("TacticsTab"));
    private readonly TabPage staffTab = new(L10n.Get("StaffTab"));
    private readonly AccentTabControl views = new() { Dock = DockStyle.Fill };
    private readonly ContextMenuStrip menu = new();
    private MySquadResult data = new();
    private bool binding;
    private bool hasSave;
    private string selectedPosition = "MC";
    private string? secondaryPosition;
    private string sortField = "MainPosition";
    private bool defaultOrder = true;
    private bool descending;
    public event Action<uint>? StaffRequested;
    public event Action<uint>? RecruitmentRequested;
    public event Action<uint>? TacticsRequested;
    public event Action? ReloadRequested;
    public event Action<uint>? PlayerOpenRequested;
    public event Action<uint>? AddComparisonRequested;
    public event Action<uint, uint>? QuickComparisonRequested;
    private sealed record PositionChoice(string Key) { public override string ToString() => Key.Length == 0 ? L10n.Get("MySquadSecondNone") : Labels.Position(Key, L10n.IsItalian); }
    private sealed record RoleChoice(string? Id)
    {
        public override string ToString() => Id is null ? L10n.Get("TacticalGeneric") : TacticalRoles.All.Single(r => r.Id == Id).Label;
    }
    private sealed record ScopeChoice(SquadScope Mode, uint? TeamId = null, string? Name = null, int? Slot = null)
    {
        public override string ToString() => TeamId.HasValue ? !string.IsNullOrWhiteSpace(Name) ? Name + " · ID " + TeamId :
            Slot.HasValue ? L10n.Format("MySquadTeamSlot", Slot.Value, TeamId) : L10n.Format("MySquadTeamId", TeamId) :
            L10n.Get(Mode == SquadScope.FirstTeam ? "MySquadFirst" : Mode == SquadScope.OtherTeams ? "MySquadOther" : "MySquadAll");
    }

    public MySquadPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var selection = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        selection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); selection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); selection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selection.Controls.Add(new Label { Text = L10n.Get("MySquadClub"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        selection.Controls.Add(club, 1, 0);
        var reload = new Button { Name = "MySquadReload", Text = L10n.Get("MySquadReload"), AutoSize = true };
        reload.Click += (_, _) => ReloadRequested?.Invoke(); selection.Controls.Add(reload, 2, 0);
        var squadFilters = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = Padding.Empty };
        squadFilters.Controls.AddRange([scope, includeLoans]);
        root.Controls.Add(selection, 0, 0); root.Controls.Add(squadFilters, 0, 1); root.Controls.Add(status, 0, 2);
        var tabs = views;
        var listTab = new TabPage(L10n.Get("MySquadRoster")); var roleTab = new TabPage(L10n.Get("MySquadBest"));
        staffTab.Controls.Add(staff); tacticsTab.Controls.Add(tactics); recruitmentTab.Controls.Add(recruitment); planningTab.Controls.Add(planning);
        recruitment.ReloadRequested += () => { recruitment.ClearForClub(SelectedClub?.ClubUid); planning.ClearMarket(); EnsureRecruitmentLoaded(); };
        tactics.PlayerOpenRequested += uid => PlayerOpenRequested?.Invoke(uid);
        tactics.AddComparisonRequested += uid => AddComparisonRequested?.Invoke(uid);
        tactics.QuickComparisonRequested += (a, b) => QuickComparisonRequested?.Invoke(a, b);
        tactics.ReloadRequested += () => { tactics.ClearForClub(SelectedClub?.ClubUid); EnsureTacticsLoaded(); };
        staff.ReloadRequested += () => { staff.ClearForClub(SelectedClub?.ClubUid); planning.ClearStaff(); EnsureStaffLoaded(); };
        planning.FindStaffRequested += (profile, job, wage, squad) =>
        {
            recruitment.ApplyPlanningSearch(profile, job, wage, squad);
            views.SelectedTab = recruitmentTab;
            EnsureRecruitmentLoaded();
        };
        planning.ReloadRequested += () =>
        {
            staff.ClearForClub(SelectedClub?.ClubUid); recruitment.ClearForClub(SelectedClub?.ClubUid);
            planning.ClearForClub(SelectedClub?.ClubUid); EnsureStaffLoaded(); EnsureRecruitmentLoaded();
        };
        tabs.SelectedIndexChanged += (_, _) => { scope.Enabled = includeLoans.Enabled = data.Status == "ready" && data.Clubs.Count > 0 && tabs.SelectedTab != staffTab && tabs.SelectedTab != recruitmentTab && tabs.SelectedTab != planningTab; EnsureStaffLoaded(); EnsureTacticsLoaded(); EnsureRecruitmentLoaded(); };
        tabs.TabPages.AddRange([listTab, roleTab, staffTab, tacticsTab, recruitmentTab, planningTab]); root.Controls.Add(tabs, 0, 3);
        var list = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(6) };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        list.RowStyles.Add(new RowStyle(SizeType.AutoSize)); list.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); list.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(count, 0, 0); footer.Controls.Add(open, 1, 0);
        list.Controls.Add(search, 0, 0); list.Controls.Add(roster, 0, 1); list.Controls.Add(footer, 0, 2); listTab.Controls.Add(list);
        string[] fields = ["Name", "Team", "MainPosition", "Age", "PositionSummary", "Ca", "Pa", "ContractEndDisplay", "ContractLabel", "Id", "Uid"];
        string[] headers = ["Text_f537c6bd038e", "Text_3b0a75ea292b", "Text_045cb480645d", "Text_9efdc62c88bf", "Text_5aa749965391", "", "", "Text_51bf843f1751", "Text_725424c55624", "Text_358796f71be1", "Text_e1e8b54d45e2"];
        int[] widths = [200, 200, 150, 60, 220, 65, 65, 115, 150, 110, 110];
        for (int i = 0; i < fields.Length; i++)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = fields[i], Name = fields[i], HeaderText = headers[i].Length == 0 ? fields[i].ToUpperInvariant() : L10n.Get(headers[i]), Width = widths[i], MinimumWidth = 50, SortMode = DataGridViewColumnSortMode.Programmatic };
            if (fields[i] is "Age" or "Ca" or "Pa" or "Id" or "Uid" or "ContractEndDisplay") c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            roster.Columns.Add(c);
        }
        roster.ColumnHeaderMouseClick += (_, e) =>
        {
            string key = roster.Columns[e.ColumnIndex].DataPropertyName;
            descending = key == sortField && !descending; sortField = key; defaultOrder = false; RefreshRoster();
        };
        roster.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || roster.Rows[e.RowIndex].DataBoundItem is not PlayerRow p) return;
            if (roster.Columns[e.ColumnIndex].DataPropertyName == "MainPosition") e.Value = PositionScheme.Compact(p.Positions, L10n.IsItalian);
            if (roster.Columns[e.ColumnIndex].DataPropertyName == "PositionSummary")
                e.Value = string.Join(", ", p.Positions.Where(x => x.Value >= 12).OrderBy(x => PositionScheme.Rank(x.Key)).Select(x => $"{Labels.PositionCode(x.Key, L10n.IsItalian)} ({x.Value})"));
            if (p.OnLoan is true)
            {
                e.CellStyle!.BackColor = Color.FromArgb(255, 247, 220); e.CellStyle.ForeColor = Color.FromArgb(97, 68, 8);
                e.CellStyle.SelectionBackColor = Color.FromArgb(143, 98, 20); e.CellStyle.SelectionForeColor = Color.White;
            }
            roster.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = p.LoanDescription;
        };
        var roleLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
        roleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        roleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35)); roleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        var field = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        field.Controls.Add(pitch);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var positionSelectors = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 2 };
        positionSelectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); positionSelectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        positionSelectors.RowStyles.Add(new RowStyle(SizeType.AutoSize)); positionSelectors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        positionSelectors.Controls.Add(position, 0, 0); positionSelectors.Controls.Add(role, 1, 0); positionSelectors.Controls.Add(secondPosition, 0, 1); positionSelectors.Controls.Add(secondRole, 1, 1);
        right.Controls.Add(positionSelectors, 0, 0); right.Controls.Add(ranking, 0, 1); right.Controls.Add(advice, 0, 2); var rankingActions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        var criteria = new Button { Name = "MySquadCriteria", Text = L10n.Get("TacticalCriteria"), AutoSize = true };
        criteria.Click += (_, _) => { using var dialog = new TacticalCriteriaForm(SelectedTargets); dialog.ShowDialog(this); };
        rankingActions.Controls.AddRange([openRanked, criteria]); right.Controls.Add(rankingActions, 0, 3);
        roleLayout.Controls.Add(field, 0, 0); roleLayout.Controls.Add(right, 1, 0); roleTab.Controls.Add(roleLayout);
        string[] rankHeaders = ["#", L10n.Get("Text_f537c6bd038e"), L10n.Get("Text_045cb480645d"), L10n.Get("MySquadScore"), L10n.Get("MySquadStars"), L10n.Get("MySquadFamiliarity"), "CA", "PA"];
        int[] rankWidths = [45, 180, 135, 100, 130, 165, 60, 60];
        for (int i = 0; i < rankHeaders.Length; i++) ranking.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = rankHeaders[i], Width = rankWidths[i], SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = i is 0 or >= 3 ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft } });
        ranking.Columns[6].Name = "Ca"; ranking.Columns[7].Name = "Pa"; ranking.Columns[7].DefaultCellStyle.NullValue = "—";
        ranking.Columns[4].CellTemplate = new StarRatingCell();
        ranking.Columns[4].DefaultCellStyle.Format = "0.0"; ranking.RowTemplate.Height = 41;
        club.SelectedIndexChanged += (_, _) => { if (!binding) { staff.ClearForClub(SelectedClub?.ClubUid); recruitment.ClearForClub(SelectedClub?.ClubUid); planning.ClearForClub(SelectedClub?.ClubUid); tactics.ClearForClub(SelectedClub?.ClubUid); RebuildScopes(reset: true); RefreshView(); } };
        includeLoans.CheckedChanged += (_, _) => { if (!binding) RefreshView(); };
        scope.SelectedIndexChanged += (_, _) => { if (!binding) RefreshView(); };
        search.TextChanged += (_, _) => { if (!binding) RefreshRoster(); };
        position.SelectedIndexChanged += (_, _) => { if (!binding && position.SelectedItem is PositionChoice p) SelectPosition(p.Key); };
        secondPosition.SelectedIndexChanged += (_, _) =>
        {
            if (binding || secondPosition.SelectedItem is not PositionChoice selected) return;
            secondaryPosition = selected.Key.Length == 0 ? null : selected.Key;
            if (secondaryPosition is null) secondaryRoleId = null;
            UpdateSelectedPositions(); RefreshRanking();
        };
        role.SelectedIndexChanged += (_, _) => { if (!binding) { selectedRoleId = (role.SelectedItem as RoleChoice)?.Id; RefreshRanking(); } };
        secondRole.SelectedIndexChanged += (_, _) => { if (!binding) { secondaryRoleId = (secondRole.SelectedItem as RoleChoice)?.Id; RefreshRanking(); } };
        pitch.PositionActivated += SelectPosition;
        roster.SelectionChanged += (_, _) => open.Enabled = SelectedUid(roster).HasValue;
        ranking.SelectionChanged += (_, _) => openRanked.Enabled = SelectedUid(ranking).HasValue;
        open.Click += (_, _) => Open(roster); openRanked.Click += (_, _) => Open(ranking);
        roster.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Open(roster); };
        ranking.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Open(ranking); };
        var quickCompare = new ToolStripMenuItem(L10n.Get("QuickComparePair")) { Name = "QuickComparePair" };
        quickCompare.Click += (_, _) => { if (menu.SourceControl is DataGridView grid && QuickComparison.TryPair(grid, out uint first, out uint second)) QuickComparisonRequested?.Invoke(first, second); };
        menu.Items.Add(quickCompare);
        var addComparison = menu.Items.Add(L10n.Get("Text_885725b27cce"), null, (_, _) => { if (menu.SourceControl is DataGridView grid && SelectedUid(grid) is uint uid) AddComparisonRequested?.Invoke(uid); });
        menu.Opening += (sender, e) =>
        {
            if (menu.SourceControl is not DataGridView grid) { e.Cancel = true; return; }
            e.Cancel = grid.SelectedRows.Count == 0;
            quickCompare.Enabled = QuickComparison.TryPair(grid, out _, out _);
            addComparison.Enabled = SelectedUid(grid).HasValue;
        };
        foreach (var grid in new[] { roster, ranking })
        {
            grid.ContextMenuStrip = menu;
            grid.CellMouseDown += (_, e) => { if (e.Button == MouseButtons.Right) QuickComparison.SelectContextRow(grid, e.RowIndex, e.ColumnIndex); };
        }
        root.SizeChanged += (_, _) => status.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 30), 0);
        right.SizeChanged += (_, _) => advice.MaximumSize = new Size(Math.Max(100, right.ClientSize.Width - 12), 0);
        Controls.Add(root);
        SetLanguage(); L10n.Capture(this);
    }

    private static DataGridView Grid(string name) => new()
    {
        Name = name, Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, MultiSelect = true,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        ScrollBars = ScrollBars.Both, AllowUserToResizeColumns = true
    };
    private static uint? SelectedUid(DataGridView grid) => grid.SelectedRows.Count != 1 ? null :
        grid.SelectedRows[0].DataBoundItem is PlayerRow p ? p.Uid : (grid.SelectedRows[0].Tag as SquadRecommendation)?.Player.Uid;
    private void Open(DataGridView grid) { if (SelectedUid(grid) is uint uid) PlayerOpenRequested?.Invoke(uid); }
    private ManagedSquad? SelectedClub => club.SelectedItem as ManagedSquad;
    private IReadOnlyList<PlayerDetail> Members => SquadSelection.Members(data, SelectedClub,
        (scope.SelectedItem as ScopeChoice)?.Mode ?? SquadScope.FirstTeam, (scope.SelectedItem as ScopeChoice)?.TeamId, includeLoans.Checked);
    private SquadTarget[] SelectedTargets => secondaryPosition is null ? [new(selectedPosition, selectedRoleId)] : [new(selectedPosition, selectedRoleId), new(secondaryPosition, secondaryRoleId)];
    private string[] SelectedPositions => secondaryPosition is null ? [selectedPosition] : [selectedPosition, secondaryPosition];
    private void RebuildScopes(bool reset = false)
    {
        var selected = reset ? null : scope.SelectedItem as ScopeChoice;
        bool wasBinding = binding; binding = true; scope.Items.Clear();
        scope.Items.AddRange([new ScopeChoice(SquadScope.FirstTeam), new ScopeChoice(SquadScope.OtherTeams), new ScopeChoice(SquadScope.All)]);
        if (SelectedClub is ManagedSquad current)
        {
            var teams = current.Teams.Concat(current.TeamIds.Where(id => current.Teams.All(t => t.TeamId != id)).Select(id => new ManagedSquadTeam { TeamId = id }));
            foreach (var team in teams.Where(t => t.TeamId != current.FirstTeamId).DistinctBy(t => t.TeamId).OrderBy(t => t.Slot ?? int.MaxValue).ThenBy(t => t.TeamId))
                scope.Items.Add(new ScopeChoice(SquadScope.All, team.TeamId, team.Name, team.Slot));
        }
        int index = selected is null ? -1 : scope.Items.Cast<ScopeChoice>().ToList().FindIndex(c => c.Mode == selected.Mode && c.TeamId == selected.TeamId);
        scope.SelectedIndex = Math.Max(0, index); binding = wasBinding;
    }

    public void ClearForSave(bool loaded)
    {
        hasSave = loaded; data = new(); defaultOrder = true; sortField = "MainPosition"; descending = false; staff.ClearForClub(null); recruitment.ClearForClub(null); planning.ClearForClub(null); tactics.ClearForClub(null); binding = true; club.Items.Clear(); search.Clear(); RebuildScopes(reset: true); binding = false; RefreshView();
    }
    public void SetData(MySquadResult result)
    {
        uint? previous = SelectedClub?.ClubUid;
        hasSave = true; data = result; binding = true; club.Items.Clear(); club.Items.AddRange(result.Clubs.Cast<object>().ToArray());
        if (club.Items.Count > 0) club.SelectedIndex = Math.Max(0, result.Clubs.FindIndex(c => c.ClubUid == previous));
        staff.ClearForClub(SelectedClub?.ClubUid); recruitment.ClearForClub(SelectedClub?.ClubUid); planning.ClearForClub(SelectedClub?.ClubUid); tactics.ClearForClub(SelectedClub?.ClubUid); RebuildScopes();
        binding = false; RefreshView();
    }
    public void SetLanguage()
    {
        L10n.Apply(this); staff.SetLanguage(); tactics.SetLanguage(); recruitment.SetLanguage(); planning.SetLanguage(); binding = true;
        RebuildScopes();
        position.Items.Clear(); position.Items.AddRange(PositionScheme.Order.Select(key => (object)new PositionChoice(key)).ToArray());
        secondPosition.Items.Clear(); secondPosition.Items.Add(new PositionChoice("")); secondPosition.Items.AddRange(PositionScheme.Order.Select(key => (object)new PositionChoice(key)).ToArray());
        pitch.SetPositions([], L10n.IsItalian);
        binding = false; UpdateSelectedPositions(); RefreshView();
    }
    public void SelectPosition(string key) => SelectPosition(key, false);
    public void SelectPosition(string key, bool extendSelection)
    {
        if (!PositionScheme.Order.Contains(key)) return;
        if (!extendSelection) { selectedPosition = key; secondaryPosition = null; secondaryRoleId = null; }
        else if (key == secondaryPosition) { secondaryPosition = null; secondaryRoleId = null; }
        else if (key == selectedPosition && secondaryPosition is not null) { selectedPosition = secondaryPosition; selectedRoleId = secondaryRoleId; secondaryPosition = null; secondaryRoleId = null; }
        else if (key != selectedPosition) secondaryPosition = key;
        UpdateSelectedPositions(); RefreshRanking();
    }
    private void UpdateSelectedPositions()
    {
        bool wasBinding = binding; binding = true;
        position.SelectedIndex = PositionScheme.Rank(selectedPosition);
        secondPosition.SelectedIndex = secondaryPosition is null ? 0 : PositionScheme.Rank(secondaryPosition) + 1;
        selectedRoleId = FillRoleChoices(role, selectedPosition, selectedRoleId);
        secondaryRoleId = FillRoleChoices(secondRole, secondaryPosition, secondaryRoleId);
        pitch.SelectedPosition = selectedPosition; pitch.SecondarySelectedPosition = secondaryPosition ?? "";
        binding = wasBinding;
    }
    private string? FillRoleChoices(ComboBox choice, string? positionKey, string? previousId)
    {
        choice.Items.Clear(); choice.Items.Add(new RoleChoice(null));
        if (positionKey is not null)
            choice.Items.AddRange(TacticalRoles.ForPosition(positionKey).OrderBy(r => r.Phase).ThenBy(r => r.Label, Labels.Comparer(L10n.IsItalian)).Select(r => (object)new RoleChoice(r.Id)).ToArray());
        int index = choice.Items.Cast<RoleChoice>().ToList().FindIndex(r => r.Id == previousId);
        choice.SelectedIndex = Math.Max(0, index); choice.Enabled = positionKey is not null;
        return (choice.SelectedItem as RoleChoice)?.Id;
    }
    private void RefreshView()
    {
        status.Text = !hasSave ? L10n.Get("MySquadOpen") : data.Status == "unavailable" ? L10n.Format("MySquadUnavailable", data.Error ?? "—") :
            data.Status != "ready" ? L10n.Get("MySquadNone") : "";
        status.Visible = status.Text.Length > 0;
        // Enabled getters include ancestor state; the main form disables tabs during loading.
        // Store the desired state from the data rather than reading a temporarily disabled peer.
        bool ready = data.Status == "ready" && data.Clubs.Count > 0;
        includeLoans.Enabled = ready && views.SelectedTab != staffTab && views.SelectedTab != recruitmentTab && views.SelectedTab != planningTab;
        roleTips.SetToolTip(includeLoans, L10n.Get("IncludeOutgoingLoansHint"));
        club.Enabled = ready; scope.Enabled = ready && views.SelectedTab != staffTab && views.SelectedTab != recruitmentTab && views.SelectedTab != planningTab;
        scope.AccessibleDescription = L10n.Get("MySquadTeamNamesHint");
        club.AccessibleDescription = data.FirstHumanOnly ? L10n.Get("MySquadFirstHuman") : "";
        roleTips.SetToolTip(scope, scope.AccessibleDescription);
        roleTips.SetToolTip(club, club.AccessibleDescription);
        RefreshRoster(); RefreshRanking(); tactics.SetMembers(Members); EnsureStaffLoaded(); EnsureTacticsLoaded(); EnsureRecruitmentLoaded();
    }
    public void RefreshOptions() => RefreshView();
    public void SetRecruitmentData(StaffRecruitmentResult result) { recruitment.SetData(result); planning.SetMarket(result); }
    public void EnsureRecruitmentLoaded()
    {
        if (!binding && (views.SelectedTab == recruitmentTab || views.SelectedTab == planningTab && staff.IsLoadedFor(SelectedClub?.ClubUid ?? 0)) && data.Status == "ready" && SelectedClub is ManagedSquad selected && !recruitment.IsLoadedFor(selected.ClubUid))
            RecruitmentRequested?.Invoke(selected.ClubUid);
    }
    public void SetStaffData(StaffResult result) { staff.SetData(result); planning.SetStaff(result); }
    public void EnsureStaffLoaded()
    {
        if (!binding && (views.SelectedTab == staffTab || views.SelectedTab == planningTab) && data.Status == "ready" && SelectedClub is ManagedSquad selected && !staff.IsLoadedFor(selected.ClubUid))
            StaffRequested?.Invoke(selected.ClubUid);
    }
    public void SetTacticsData(SavedTacticsResult result) => tactics.SetData(result);
    public void EnsureTacticsLoaded()
    {
        if (!binding && views.SelectedTab == tacticsTab && data.Status == "ready" && SelectedClub is ManagedSquad selected && !tactics.IsLoadedFor(selected.ClubUid))
            TacticsRequested?.Invoke(selected.ClubUid);
    }
    private void RefreshRoster()
    {
        var members = Members;
        string text = search.Text.Trim();
        var rows = members.Select(p => p.Player).Where(p => text.Length == 0 || p.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
            p.Uid.ToString(CultureInfo.InvariantCulture).Contains(text) || p.Id?.ToString(CultureInfo.InvariantCulture).Contains(text) == true).ToArray();
        object? Value(PlayerRow p) => sortField switch
        {
            "Team" => p.Team, "MainPosition" => PositionScheme.Rank(p.MainPosition), "Age" => p.Age,
            "Ca" => p.Ca, "Pa" => p.Pa, "Id" => p.Id, "Uid" => p.Uid, "ContractEndDisplay" => p.ContractEnd,
            "ContractLabel" => p.ContractLabel, "PositionSummary" => p.PositionSummary, _ => p.Name
        };
        var known = rows.Where(p => Value(p) is not null);
        var sorted = defaultOrder ? SquadSelection.DefaultOrder(rows).ToList() : (descending ? known.OrderByDescending(Value) : known.OrderBy(Value)).ThenBy(p => p.Uid)
            .Concat(rows.Where(p => Value(p) is null)).ToList();
        roster.DataSource = new BindingList<PlayerRow>(sorted); roster.ClearSelection(); open.Enabled = false;
        foreach (DataGridViewColumn c in roster.Columns) c.HeaderCell.SortGlyphDirection = defaultOrder && c.DataPropertyName == "Ca" ? SortOrder.Descending : c.DataPropertyName == sortField ? descending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
        count.Text = L10n.Format("MySquadCount", sorted.Count, members.Count);
    }
    private void RefreshRanking()
    {
        ranking.Rows.Clear();
        var members = Members;
        var best = SquadSelection.BestForRoles(members, SelectedTargets);
        for (int i = 0; i < best.Count; i++)
        {
            var p = best[i]; int at = ranking.Rows.Add(i + 1, p.Player.Name, PositionScheme.Compact(p.Player.Positions, L10n.IsItalian), p.Score, p.Stars, string.Join(" · ", p.Familiarities.Select(pair => Labels.PositionCode(pair.Key, L10n.IsItalian) + ": " + pair.Value)), p.Player.Ca, p.Player.Pa);
            ranking.Rows[at].Tag = p;
            ranking.Rows[at].Cells[3].ToolTipText = string.Join(" · ", p.PositionScores.Select(pair => TacticalRoles.LabelKey(pair.Key) + ": " + pair.Value.ToString("0.00", L10n.Culture) + "/20"));
            ranking.Rows[at].Cells[4].ToolTipText = L10n.Format("MySquadStarsValue", p.Stars);
        }
        ranking.Columns[3].DefaultCellStyle.Format = "0.00";
        ranking.Columns[3].DefaultCellStyle.FormatProvider = L10n.Culture;
        ranking.ClearSelection(); openRanked.Enabled = false;
        roleTips.SetToolTip(role, TargetTip(SelectedTargets[0]));
        roleTips.SetToolTip(secondRole, secondaryPosition is null ? L10n.Get("TacticalSelectSecond") : TargetTip(SelectedTargets[1]));
        advice.Text = string.Join(" + ", SelectedTargets.Select(TacticalRoles.Label)) + "\n" + L10n.Format("MySquadRankingInfo", best.Count, members.Count) + "\n" + L10n.Get("MySquadMethod") + (SelectedTargets.Any(t => t.RoleId is not null) ? "\n" + L10n.Get("TacticalEstimateShort") : "");
    }
    private static string TargetTip(SquadTarget target) => TacticalRoles.Label(target) + "\n" + L10n.Get("TacticalEstimateShort") + "\n" +
        string.Join("\n", TacticalRoles.Weights(target).OrderByDescending(f => f.Value).ThenBy(f => Labels.Attribute(f.Key, L10n.IsItalian)).Select(f => Labels.Attribute(f.Key, L10n.IsItalian) + " ×" + f.Value));
    protected override void Dispose(bool disposing) { if (disposing) { menu.Dispose(); roleTips.Dispose(); } base.Dispose(disposing); }
}
