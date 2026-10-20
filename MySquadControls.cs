using System.ComponentModel;
using System.Globalization;

namespace FM26SimpleEditor;

public sealed class MySquadPanel : UserControl
{
    private readonly ComboBox club = new() { Name = "ManagedClubChoice", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox scope = new() { Name = "MySquadScope", DropDownStyle = ComboBoxStyle.DropDownList, Width = 270 };
    private readonly TextBox search = new() { Name = "MySquadSearch", Dock = DockStyle.Fill, PlaceholderText = L10n.Get("MySquadSearch") };
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label count = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly DataGridView roster = Grid("MySquadRoster");
    private readonly DataGridView ranking = Grid("MySquadRanking");
    private readonly PitchControl pitch = new() { SelectionEnabled = true, SelectedPosition = "MC", MinimumSize = new Size(230, 350) };
    private readonly ComboBox position = new() { Name = "MySquadPosition", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly Label advice = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Button open = new() { Text = L10n.Get("Text_9337b648a0e1"), AutoSize = true };
    private readonly Button openRanked = new() { Text = L10n.Get("Text_9337b648a0e1"), AutoSize = true };
    private readonly StaffPanel staff = new();
    private readonly TabPage staffTab = new(L10n.Get("StaffTab"));
    private readonly TabControl views = new() { Dock = DockStyle.Fill };
    private readonly ContextMenuStrip menu = new();
    private MySquadResult data = new();
    private bool binding;
    private bool hasSave;
    private string selectedPosition = "MC";
    private string sortField = "Name";
    private bool descending;
    public event Action<uint>? StaffRequested;
    public event Action? ReloadRequested;
    public event Action<uint>? PlayerOpenRequested;
    public event Action<uint>? AddComparisonRequested;
    private sealed record PositionChoice(string Key) { public override string ToString() => Labels.Position(Key, L10n.IsItalian); }

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
        root.Controls.Add(selection, 0, 0); root.Controls.Add(scope, 0, 1); root.Controls.Add(status, 0, 2);
        var tabs = views;
        var listTab = new TabPage(L10n.Get("MySquadRoster")); var roleTab = new TabPage(L10n.Get("MySquadBest"));
        staffTab.Controls.Add(staff);
        staff.ReloadRequested += () => { staff.ClearForClub(SelectedClub?.ClubUid); EnsureStaffLoaded(); };
        tabs.SelectedIndexChanged += (_, _) => { scope.Enabled = data.Status == "ready" && tabs.SelectedTab != staffTab; EnsureStaffLoaded(); };
        tabs.TabPages.AddRange([listTab, roleTab, staffTab]); root.Controls.Add(tabs, 0, 3);
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
            descending = key == sortField && !descending; sortField = key; RefreshRoster();
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
        var role = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
        role.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        role.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35)); role.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        var field = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        field.Controls.Add(pitch);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.Controls.Add(position, 0, 0); right.Controls.Add(ranking, 0, 1); right.Controls.Add(advice, 0, 2); right.Controls.Add(openRanked, 0, 3);
        role.Controls.Add(field, 0, 0); role.Controls.Add(right, 1, 0); roleTab.Controls.Add(role);
        string[] rankHeaders = ["#", L10n.Get("Text_f537c6bd038e"), L10n.Get("Text_045cb480645d"), L10n.Get("MySquadScore"), L10n.Get("MySquadFamiliarity"), "CA"];
        int[] rankWidths = [45, 180, 135, 100, 110, 60];
        for (int i = 0; i < rankHeaders.Length; i++) ranking.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = rankHeaders[i], Width = rankWidths[i], SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = i is 0 or >= 3 ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft } });
        ranking.Columns[4].CellTemplate = new ValueBarCell(20); ranking.RowTemplate.Height = 41;
        club.SelectedIndexChanged += (_, _) => { if (!binding) { staff.ClearForClub(SelectedClub?.ClubUid); RefreshView(); } };
        scope.SelectedIndexChanged += (_, _) => { if (!binding) RefreshView(); };
        search.TextChanged += (_, _) => { if (!binding) RefreshRoster(); };
        position.SelectedIndexChanged += (_, _) => { if (!binding && position.SelectedItem is PositionChoice p) SelectPosition(p.Key); };
        pitch.PositionSelected += SelectPosition;
        roster.SelectionChanged += (_, _) => open.Enabled = SelectedUid(roster).HasValue;
        ranking.SelectionChanged += (_, _) => openRanked.Enabled = SelectedUid(ranking).HasValue;
        open.Click += (_, _) => Open(roster); openRanked.Click += (_, _) => Open(ranking);
        roster.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Open(roster); };
        ranking.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Open(ranking); };
        menu.Items.Add(L10n.Get("Text_885725b27cce"), null, (_, _) => { if (menu.SourceControl is DataGridView grid && SelectedUid(grid) is uint uid) AddComparisonRequested?.Invoke(uid); });
        menu.Opening += (_, e) => e.Cancel = menu.SourceControl is not DataGridView grid || SelectedUid(grid) is null;
        foreach (var grid in new[] { roster, ranking })
        {
            grid.ContextMenuStrip = menu;
            grid.CellMouseDown += (_, e) => { if (e.Button == MouseButtons.Right && e.RowIndex >= 0) { grid.ClearSelection(); grid.CurrentCell = grid.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)]; grid.Rows[e.RowIndex].Selected = true; } };
        }
        root.SizeChanged += (_, _) => status.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 30), 0);
        right.SizeChanged += (_, _) => advice.MaximumSize = new Size(Math.Max(100, right.ClientSize.Width - 12), 0);
        Controls.Add(root);
        SetLanguage(); L10n.Capture(this);
    }

    private static DataGridView Grid(string name) => new()
    {
        Name = name, Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, MultiSelect = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        ScrollBars = ScrollBars.Both, AllowUserToResizeColumns = true
    };
    private static uint? SelectedUid(DataGridView grid) => grid.SelectedRows.Count != 1 ? null :
        grid.SelectedRows[0].DataBoundItem is PlayerRow p ? p.Uid : (grid.SelectedRows[0].Tag as SquadRecommendation)?.Player.Uid;
    private void Open(DataGridView grid) { if (SelectedUid(grid) is uint uid) PlayerOpenRequested?.Invoke(uid); }
    private ManagedSquad? SelectedClub => club.SelectedItem as ManagedSquad;
    private IReadOnlyList<PlayerDetail> Members => SquadSelection.Members(data, SelectedClub, (SquadScope)Math.Max(0, scope.SelectedIndex));

    public void ClearForSave(bool loaded)
    {
        hasSave = loaded; data = new(); staff.ClearForClub(null); binding = true; club.Items.Clear(); search.Clear(); scope.SelectedIndex = 0; binding = false; RefreshView();
    }
    public void SetData(MySquadResult result)
    {
        uint? previous = SelectedClub?.ClubUid;
        hasSave = true; data = result; binding = true; club.Items.Clear(); club.Items.AddRange(result.Clubs.Cast<object>().ToArray());
        if (club.Items.Count > 0) club.SelectedIndex = Math.Max(0, result.Clubs.FindIndex(c => c.ClubUid == previous));
        staff.ClearForClub(SelectedClub?.ClubUid);
        binding = false; RefreshView();
    }
    public void SetLanguage()
    {
        L10n.Apply(this); staff.SetLanguage(); binding = true;
        int old = Math.Max(0, scope.SelectedIndex); scope.Items.Clear();
        scope.Items.AddRange([L10n.Get("MySquadFirst"), L10n.Get("MySquadOther"), L10n.Get("MySquadAll")]); scope.SelectedIndex = old;
        position.Items.Clear(); position.Items.AddRange(PositionScheme.Order.Select(key => (object)new PositionChoice(key)).ToArray());
        position.SelectedIndex = PositionScheme.Rank(selectedPosition); pitch.SetPositions([], L10n.IsItalian);
        binding = false; RefreshView();
    }
    public void SelectPosition(string key)
    {
        if (!PositionScheme.Order.Contains(key)) return;
        selectedPosition = key; pitch.SelectedPosition = key;
        binding = true; position.SelectedIndex = PositionScheme.Rank(key); binding = false; RefreshRanking();
    }
    private void RefreshView()
    {
        status.Text = !hasSave ? L10n.Get("MySquadOpen") : data.Status == "unavailable" ? L10n.Format("MySquadUnavailable", data.Error ?? "—") :
            data.Status != "ready" ? L10n.Get("MySquadNone") : data.FirstHumanOnly ? L10n.Get("MySquadFirstHuman") : "";
        club.Enabled = data.Status == "ready" && data.Clubs.Count > 0;
        scope.Enabled = club.Enabled && views.SelectedTab != staffTab;
        RefreshRoster(); RefreshRanking(); EnsureStaffLoaded();
    }
    public void SetStaffData(StaffResult result) => staff.SetData(result);
    public void EnsureStaffLoaded()
    {
        if (!binding && views.SelectedTab == staffTab && data.Status == "ready" && SelectedClub is ManagedSquad selected && !staff.IsLoadedFor(selected.ClubUid))
            StaffRequested?.Invoke(selected.ClubUid);
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
        var sorted = (descending ? known.OrderByDescending(Value) : known.OrderBy(Value)).ThenBy(p => p.Uid)
            .Concat(rows.Where(p => Value(p) is null)).ToList();
        roster.DataSource = new BindingList<PlayerRow>(sorted); roster.ClearSelection(); open.Enabled = false;
        foreach (DataGridViewColumn c in roster.Columns) c.HeaderCell.SortGlyphDirection = c.DataPropertyName == sortField ? descending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
        count.Text = L10n.Format("MySquadCount", sorted.Count, members.Count);
    }
    private void RefreshRanking()
    {
        ranking.Rows.Clear();
        var members = Members;
        var best = SquadSelection.TopThree(members, selectedPosition);
        for (int i = 0; i < best.Count; i++)
        {
            var p = best[i]; int at = ranking.Rows.Add(i + 1, p.Player.Name, PositionScheme.Compact(p.Player.Positions, L10n.IsItalian), p.Score, p.Familiarity, p.Player.Ca);
            ranking.Rows[at].Tag = p;
        }
        ranking.Columns[3].DefaultCellStyle.Format = "0.00";
        ranking.Columns[3].DefaultCellStyle.FormatProvider = L10n.Culture;
        ranking.ClearSelection(); openRanked.Enabled = false;
        advice.Text = L10n.Format("MySquadRankingInfo", best.Count, members.Count) + "\n" + L10n.Get("MySquadMethod");
    }
    protected override void Dispose(bool disposing) { if (disposing) menu.Dispose(); base.Dispose(disposing); }
}
