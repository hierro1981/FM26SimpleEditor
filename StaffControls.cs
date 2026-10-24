using System.ComponentModel;
using System.Globalization;

namespace FM26SimpleEditor;

public sealed class StaffPanel : UserControl
{
    private readonly TextBox search = new() { Name = "StaffSearch", Dock = DockStyle.Fill, PlaceholderText = L10n.Get("StaffSearch") };
    private readonly Label status = new() { Name = "StaffStatus", AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label count = new() { Name = "StaffCount", AutoSize = true };
    private readonly DataGridView list = Grid("StaffList");
    private readonly DataGridView details = Grid("StaffDetails");
    private readonly DataGridView coaching = Grid("StaffCoachingStars");
    private StaffResult? data;
    private uint? clubUid;
    private string sortField = "Name";
    private bool descending;
    public bool IsLoadedFor(uint id) => clubUid == id && data is not null;
    public event Action? ReloadRequested;

    public StaffPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), RowCount = 5, ColumnCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        var tools = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var reload = new Button { Text = L10n.Get("StaffReload"), AutoSize = true };
        reload.Click += (_, _) => ReloadRequested?.Invoke(); tools.Controls.Add(search, 0, 0); tools.Controls.Add(reload, 1, 0);
        var coachingNote = new Label { AutoSize = true, Dock = DockStyle.Fill, ForeColor = Color.DimGray, Text = L10n.Get("StaffCoachingEstimate") };
        var coachingPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        coachingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        coachingPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); coachingPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        coachingPanel.Controls.Add(coachingNote, 0, 0); coachingPanel.Controls.Add(coaching, 0, 1);
        var detailArea = new TableLayoutPanel { Name = "StaffDetailColumns", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        detailArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); detailArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        detailArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var attributeGroup = new GroupBox { Name = "StaffAttributeGroup", Text = L10n.Get("StaffAttributes"), Dock = DockStyle.Fill };
        var coachingGroup = new GroupBox { Name = "StaffCoachingGroup", Text = L10n.Get("StaffCoachingStars"), Dock = DockStyle.Fill };
        attributeGroup.Controls.Add(details); coachingGroup.Controls.Add(coachingPanel);
        detailArea.Controls.Add(attributeGroup, 0, 0); detailArea.Controls.Add(coachingGroup, 1, 0);
        coachingPanel.SizeChanged += (_, _) => coachingNote.MaximumSize = new Size(Math.Max(100, coachingPanel.ClientSize.Width - 12), 0);
        root.Controls.Add(tools, 0, 0); root.Controls.Add(status, 0, 1); root.Controls.Add(list, 0, 2); root.Controls.Add(count, 0, 3); root.Controls.Add(detailArea, 0, 4);
        (string Field, string Header, int Width)[] columns =
        [
            ("Name", "Text_f537c6bd038e", 190), ("RoleDisplay", "StaffRole", 225),
            ("CurrentRoleStars", "StaffCurrentRoleStars", 225),
            ("ClubName", "StaffClub", 180), ("ListedClubName", "StaffListedClub", 180),
            ("Age", "Text_9efdc62c88bf", 60), ("BirthDisplay", "StaffBirth", 105),
            ("Ca", "", 65), ("Pa", "", 65), ("CurrentReputation", "StaffPersonalReputation", 180), ("Wage", "StaffWage", 160),
            ("ContractDisplay", "StaffContract", 155), ("ContractStartDisplay", "StaffContractStart", 110),
            ("ContractEndDisplay", "Text_51bf843f1751", 110), ("Id", "Text_358796f71be1", 110), ("Uid", "Text_e1e8b54d45e2", 110)
        ];
        foreach (var c in columns)
        {
            var column = new DataGridViewTextBoxColumn { Name = c.Field, DataPropertyName = c.Field, HeaderText = c.Header.Length == 0 ? c.Field.ToUpperInvariant() : L10n.Get(c.Header), Width = c.Width, MinimumWidth = 50, SortMode = DataGridViewColumnSortMode.Programmatic };
            column.DefaultCellStyle.NullValue = "—";
            if (c.Field is "Age" or "Ca" or "Pa" or "Wage" or "CurrentReputation" or "Id" or "Uid" || c.Field.EndsWith("Display") && c.Field != "RoleDisplay" && c.Field != "ContractDisplay") column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (c.Field == "Wage") column.DefaultCellStyle.Format = "N0";
            if (c.Field == "CurrentRoleStars")
            {
                column.CellTemplate = new StarRatingCell();
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.ToolTipText = L10n.Get("StaffCurrentRoleStarsNote");
            }
            list.Columns.Add(column);
        }
        list.RowTemplate.Height = 38;
        details.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.Get("StaffGroup"), Width = 180 });
        details.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.Get("Text_51df0484073a"), Width = 245 });
        details.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.Get("StaffValue"), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        foreach (DataGridViewColumn column in details.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
        string[] coachingHeaders = ["StaffCoachingCategory", "StaffCoachingStars", "StaffCoachingScore", "StaffCoachingCalculation"];
        int[] coachingWidths = [200, 165, 100, 650];
        for (int i = 0; i < coachingHeaders.Length; i++) coaching.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = L10n.Get(coachingHeaders[i]), Width = coachingWidths[i], SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = i is 1 or 2 ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft }
        });
        coaching.RowTemplate.Height = 38;
        list.ColumnHeaderMouseClick += (_, e) => SortBy(list.Columns[e.ColumnIndex].DataPropertyName);
        list.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && list.Columns[e.ColumnIndex].Name == "RoleDisplay" &&
                list.Rows[e.RowIndex].DataBoundItem is StaffRow person) e.ToolTipText = person.RoleTooltip;
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && list.Columns[e.ColumnIndex].Name == "CurrentRoleStars")
                e.ToolTipText = list.Rows[e.RowIndex].DataBoundItem is StaffRow rated ? StaffRecruitment.CurrentStarsTooltip(rated) : L10n.Get("StaffCurrentRoleStarsNote");
        };
        list.SelectionChanged += (_, _) => ShowDetails(); search.TextChanged += (_, _) => RefreshRows();
        root.SizeChanged += (_, _) => status.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 20), 0);
        Controls.Add(root); RefreshRows(); L10n.Capture(this);
    }
    private static DataGridView Grid(string name) => new()
    {
        Name = name, Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, MultiSelect = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        ScrollBars = ScrollBars.Both, AllowUserToResizeColumns = true
    };
    public void ClearForClub(uint? id)
    {
        if (id != clubUid) search.Clear();
        clubUid = id; data = null; RefreshRows();
    }
    public void SetData(StaffResult result)
    {
        if (result.ClubUid != clubUid) return; // Ignore stale results from another club/save.
        data = result; RefreshRows();
    }
    public void SetLanguage() { L10n.Apply(this); list.Columns["CurrentRoleStars"].ToolTipText = L10n.Get("StaffCurrentRoleStarsNote"); RefreshRows(); }
    public void SortBy(string field)
    {
        if (!list.Columns.Contains(field)) return;
        descending = sortField == field && !descending; sortField = field; RefreshRows();
    }
    private StaffRow? Selected => list.SelectedRows.Count == 1 ? list.SelectedRows[0].DataBoundItem as StaffRow : null;
    private void RefreshRows()
    {
        var selected = Selected;
        string text = search.Text.Trim();
        var rows = (data?.Staff ?? []).Where(p => data!.Status == "ready" && (text.Length == 0 || p.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
            p.RoleDisplay.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
            p.ClubName.Contains(text, StringComparison.CurrentCultureIgnoreCase) || p.ListedClubName?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true ||
            p.Uid.ToString(CultureInfo.InvariantCulture).Contains(text) || p.Id?.ToString(CultureInfo.InvariantCulture).Contains(text) == true)).ToArray();
        object? Value(StaffRow p) => sortField switch
        {
            "RoleDisplay" => p.RoleDisplay, "ClubName" => p.ClubName, "ListedClubName" => p.ListedClubName,
            "Age" => p.Age, "BirthDisplay" => p.Birth, "Ca" => p.Ca, "Pa" => p.Pa, "Wage" => p.Wage,
            "CurrentRoleStars" => p.CurrentRoleStars, "CurrentReputation" => p.CurrentReputation,
            "ContractDisplay" => p.ContractDisplay, "ContractStartDisplay" => p.ContractStart, "ContractEndDisplay" => p.ContractEnd,
            "Id" => p.Id, "Uid" => p.Uid, _ => p.Name
        };
        var known = rows.Where(p => Value(p) is not null);
        var sorted = (descending ? known.OrderByDescending(Value) : known.OrderBy(Value)).ThenBy(p => p.Uid).ThenBy(p => p.ClubUid)
            .Concat(rows.Where(p => Value(p) is null)).ToList();
        list.DataSource = new BindingList<StaffRow>(sorted); list.ClearSelection();
        foreach (DataGridViewColumn column in list.Columns)
        {
            column.HeaderCell.SortGlyphDirection = column.DataPropertyName == sortField ? descending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
            column.DefaultCellStyle.FormatProvider = L10n.Culture;
        }
        if (selected is not null)
        {
            int index = sorted.FindIndex(p => p.Uid == selected.Uid && p.ClubUid == selected.ClubUid);
            if (index >= 0) list.Rows[index].Selected = true;
        }
        status.Text = clubUid is null ? L10n.Get("StaffSelectClub") : data is null ? L10n.Get("StaffPending") :
            data.Status == "unavailable" ? L10n.Format("StaffUnavailable", data.Error ?? "—") :
            data.Status != "ready" ? L10n.Get("StaffSelectClub") : L10n.Get("StaffReadOnlyNote");
        count.Text = L10n.Format("StaffCount", sorted.Count, data?.Staff.Count ?? 0);
        ShowDetails();
    }
    private void ShowDetails()
    {
        details.Rows.Clear();
        coaching.Rows.Clear();
        if (Selected is not StaffRow person) return;
        foreach (var rating in CoachingStars.Calculate(person))
        {
            string calculation = CoachingStars.Calculation(person, rating);
            int row = coaching.Rows.Add(L10n.Get("CoachingCategory_" + rating.Category.Key), "—", rating.Score?.ToString(L10n.Culture) ?? "—", calculation);
            if (rating.Stars is double stars) coaching.Rows[row].Cells[1] = new StarRatingCell { Value = stars };
            string tooltip = L10n.Get("StaffCoachingEstimate") + "\n" + calculation +
                (rating.Score.HasValue ? " = " + rating.Score + "/300\n" + L10n.Format("MySquadStarsValue", rating.Stars) : "\n" + L10n.Get("StaffCoachingMissing"));
            foreach (DataGridViewCell cell in coaching.Rows[row].Cells) cell.ToolTipText = tooltip;
        }
        void Add(string group, string prefix, Dictionary<string, int> values)
        {
            foreach (var pair in values.OrderBy(x => L10n.Get(prefix + x.Key), Labels.Comparer(L10n.IsItalian)))
                details.Rows.Add(L10n.Get(group), L10n.Get(prefix + pair.Key), pair.Value is >= 1 and <= 20 ? pair.Value : "—");
        }
        foreach (var pair in person.Reputation.Where(p => p.Value is >= 1 and <= 10000))
            details.Rows.Add(L10n.Get("StaffReputation"), L10n.Get("StaffReputation_" + pair.Key), pair.Value);
        foreach (var pair in person.JobPreferences.Where(p => p.Value is >= 1 and <= 20 && L10n.Keys.Contains("StaffJob_" + p.Key)))
            details.Rows.Add(L10n.Get("StaffJobPreferences"), L10n.Get("StaffJob_" + pair.Key), pair.Value);
        Add("StaffPersonality", "StaffPersonality_", person.Personality);
        Add("StaffAttributes", "StaffAttribute_", person.Attributes);
        Add("StaffPreferences", "StaffPreference_", person.Preferences);
        details.ClearSelection();
        coaching.ClearSelection();
    }
}
