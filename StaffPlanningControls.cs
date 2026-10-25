using System.ComponentModel;

namespace FM26SimpleEditor;

public sealed class StaffPlanningPanel : UserControl
{
    private readonly TextBox search = new() { Name = "PlanSearch", Width = 230, PlaceholderText = L10n.Get("StaffSearch") };
    private readonly NumericUpDown offer = new() { Name = "PlanOffer", Minimum = 0, Maximum = 10000000, Increment = 100, ThousandsSeparator = true, Width = 125 };
    private readonly ComboBox criterion = new() { Name = "PlanCriterion", DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, DropDownWidth = 450 };
    private readonly ComboBox expiry = new() { Name = "PlanExpiry", DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, DropDownWidth = 300 };
    private string alternativeSort = "Quality";
    private bool alternativeDescending = true;
    private readonly Button findStaff = new() { Name = "PlanFindStaff", Text = L10n.Get("PlanFindStaff"), AutoSize = true };
    private readonly Label status = new() { Name = "PlanStatus", Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label explanation = new() { Name = "PlanExplanation", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
    private readonly DataGridView list = Grid("PlanList");
    private readonly DataGridView alternatives = Grid("PlanAlternatives");
    private readonly DataGridView attributes = Grid("PlanAttributes");
    private readonly ToolTip tips = new() { AutoPopDelay = 30000 };
    private readonly Dictionary<uint, string> choices = new();
    private IReadOnlyList<StaffRenewal>? cached;
    private StaffResult? own;
    private StaffRecruitmentResult? market;
    private uint? clubUid;
    private bool binding;
    private string sort = "ContractEnd";
    private bool descending;
    public event Action? ReloadRequested;
    public event Action<StaffSearchProfile, string?, long, string, int?>? FindStaffRequested;
    public StaffPlanningPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(6) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        var reload = new Button { Text = L10n.Get("StaffReload"), AutoSize = true };
        reload.Click += (_, _) => ReloadRequested?.Invoke();
        var method = new Button { Name = "PlanMethod", Text = L10n.Get("TacticalCriteria"), AutoSize = true };
        method.Click += (_, _) => MessageBox.Show(this, L10n.Get("PlanMethod"), L10n.Get("PlanTab"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        tools.Controls.AddRange([search, new Label { Text = L10n.Get("PlanExpiry"), AutoSize = true, Margin = new Padding(6,8,0,0) }, expiry, new Label { Text = L10n.Get("RecruitOffer"), AutoSize = true, Margin = new Padding(6,8,0,0) }, offer, reload, method]);
        findStaff.Click += (_, _) => OpenRecruitment(Selected);
        root.Controls.Add(tools, 0, 0); root.Controls.Add(status, 0, 1); root.Controls.Add(list, 0, 2);
        var detail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize)); detail.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); detail.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
        var profileTools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        profileTools.Controls.AddRange([new Label { Text = L10n.Get("PlanCriterion"), AutoSize = true, Margin = new Padding(6,8,0,0) }, criterion, findStaff]);
        detail.Controls.Add(profileTools, 0, 0);
        var reasonScroll = new Panel { Name = "PlanReasonScroll", Dock = DockStyle.Fill, AutoScroll = true };
        reasonScroll.Controls.Add(explanation); detail.Controls.Add(reasonScroll, 0, 1);
        var columns = new TableLayoutPanel { Name = "PlanDetailColumns", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35)); columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var altGroup = new GroupBox { Dock = DockStyle.Fill, Text = L10n.Get("PlanAlternatives") }; altGroup.Controls.Add(alternatives);
        var attrGroup = new GroupBox { Dock = DockStyle.Fill, Text = L10n.Get("PlanAttributeComparison") }; attrGroup.Controls.Add(attributes);
        columns.Controls.Add(altGroup, 0, 0); columns.Controls.Add(attrGroup, 1, 0); detail.Controls.Add(columns, 0, 2);
        root.Controls.Add(detail, 0, 3); Controls.Add(root);
        Add(list, "Name", "Text_f537c6bd038e", 190); CopyColumn(list); InfoColumn(list);
        Add(list, "Role", "StaffRole", 230); Add(list, "Team", "StaffTeam", 210);
        Add(list, "ContractEnd", "Text_51bf843f1751", 110, true); Add(list, "Days", "PlanDays", 85, true);
        Add(list, "Recommendation", "PlanRecommendation", 240);
        Add(list, "Quality", "RecruitQuality", 95, true); Add(list, "Stars", "RecruitRoleStars", 170, true, true);
        Add(list, "Ca", "", 60, true); Add(list, "Pa", "", 60, true);
        Add(list, "Wage", "StaffWage", 145, true); Add(list, "Replacement", "PlanBestAlternative", 190); Add(list, "ReplacementStars", "PlanReplacementStars", 180, true, true); Add(list, "ReplacementTeam", "PlanReplacementTeam", 210); Add(list, "BetterCount", "PlanBetterCount", 100, true);
        Add(list, "Criterion", "PlanCriterion", 130);
        list.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        list.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        foreach (var (field, minimum) in new (string, int)[]
            { ("Role",100), ("Team",80), ("ContractEnd",90), ("Days",55), ("Criterion",110),
              ("Quality",60), ("Replacement",110), ("ReplacementTeam",80), ("BetterCount",65) })
        {
            list.Columns[field].MinimumWidth = minimum;
            list.Columns[field].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
        }
        list.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && list.Columns[e.ColumnIndex].Name is not ("CopyName" or "Info"))
                OpenRecruitment(list.Rows[e.RowIndex].DataBoundItem as StaffRenewal);
        };
        Add(alternatives, "Name", "Text_f537c6bd038e", 180); CopyColumn(alternatives); InfoColumn(alternatives);
        Add(alternatives, "Club", "StaffClub", 170); Add(alternatives, "CurrentRole", "StaffRole", 220); Add(alternatives, "Team", "StaffTeam", 210);
        Add(alternatives, "Quality", "RecruitQuality", 95, true); Add(alternatives, "RoleStars", "RecruitRoleStars", 170, true, true);
        Add(alternatives, "Ca", "", 60, true); Add(alternatives, "Pa", "", 60, true);
        Add(alternatives, "InterestLabel", "RecruitInterest", 220); Add(alternatives, "ClubReputation", "RecruitClubReputation", 150, true);
        Add(alternatives, "Wage", "StaffWage", 145, true); Add(alternatives, "MainAttributes", "RecruitAttributes", 650);
        Add(attributes, "Attribute", "Text_51df0484073a", 190); Add(attributes, "Current", "PlanCurrent", 100, true);
        Add(attributes, "Alternative", "PlanAlternative", 110, true); Add(attributes, "Delta", "PlanGain", 85, true);
        foreach (var grid in new[] { list, alternatives })
        {
            grid.RowTemplate.Height = 38;
            grid.Columns["Quality"].DefaultCellStyle.Format = "0.00"; grid.Columns["Wage"].DefaultCellStyle.Format = "N0";
            grid.CellContentClick += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "CopyName") return;
                var name = grid.Rows[e.RowIndex].DataBoundItem switch { StaffRenewal p => p.Name, StaffCandidate c => c.Name, _ => "" };
                if (name.Length > 0) Clipboard.SetText(name);
            };
        }
        list.ColumnHeaderMouseClick += (_, e) =>
        {
            if (list.Columns[e.ColumnIndex].Name is "CopyName" or "Info") return;
            var field = list.Columns[e.ColumnIndex].Name; descending = sort == field && !descending; sort = field; RefreshRows();
        };
        list.SelectionChanged += (_, _) => { if (!binding) Details(); };
        alternatives.SelectionChanged += (_, _) => { if (!binding) AttributeComparison(); };
        list.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && list.Columns[e.ColumnIndex].Name == "Recommendation" && list.Rows[e.RowIndex].DataBoundItem is StaffRenewal row)
                e.CellStyle!.ForeColor = row.Advice switch { StaffRenewalAdvice.Renew => Color.DarkGreen, StaffRenewalAdvice.CompareReplacement => Color.DarkOrange, StaffRenewalAdvice.VerifyAlternatives => Color.DarkGoldenrod, _ => Color.DimGray };
        };
        alternatives.ColumnHeaderMouseClick += (_, e) =>
        {
            var field = alternatives.Columns[e.ColumnIndex].Name;
            if (field is "CopyName" or "Info") return;
            alternativeDescending = alternativeSort == field ? !alternativeDescending : field is "Quality" or "RoleStars" or "ClubReputation" or "Wage" or "Ca" or "Pa";
            alternativeSort = field; RefreshAlternatives();
        };
        foreach (var grid in new[] { list, alternatives })
        {
            // Native grid tooltips also appear for clipped cells. Disable those: only the i icon opens details.
            grid.ShowCellToolTips = false;
            grid.CellMouseEnter += (_, e) =>
            {
                tips.Hide(grid);
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Info") return;
                string text = InfoText(grid.Rows[e.RowIndex].DataBoundItem);
                var bounds = grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                tips.Show(text, grid, Math.Max(0, bounds.Left), bounds.Bottom, 20000);
            };
            grid.CellMouseLeave += (_, _) => tips.Hide(grid);
            grid.MouseLeave += (_, _) => tips.Hide(grid);
            grid.Scroll += (_, _) => tips.Hide(grid);
        }
        expiry.SelectedIndexChanged += (_, _) => { if (!binding) { cached = null; RefreshRows(); } };
        criterion.SelectedIndexChanged += (_, _) =>
        {
            if (binding || Selected is not StaffRenewal row || criterion.SelectedItem is not StaffSearchProfile p) return;
            choices[row.Uid] = p.Key; cached = null; RefreshRows();
        };
        search.TextChanged += (_, _) => RefreshRows(); offer.ValueChanged += (_, _) => { cached = null; RefreshRows(); };
        root.SizeChanged += (_, _) => status.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 20), 0);
        reasonScroll.SizeChanged += (_, _) => explanation.MaximumSize = new Size(Math.Max(100, reasonScroll.ClientSize.Width - 24), 0);
        L10n.Capture(this); SetLanguage();
    }
    private void OpenRecruitment(StaffRenewal? row)
    {
        if (row?.Profile is StaffSearchProfile profile)
            FindStaffRequested?.Invoke(profile, row.Person.JobKey, (long)offer.Value, StaffSquads.TargetFor(row.Person), StaffRecruitment.AnalystJob(row.Person));
    }
    private StaffRenewal? Selected => list.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault()?.DataBoundItem as StaffRenewal;
    private static DataGridView Grid(string name) => new() { Name = name, Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ScrollBars = ScrollBars.Both, BackgroundColor = SystemColors.Window };
    private static void Add(DataGridView grid, string field, string header, int width, bool numeric = false, bool stars = false)
    {
        var column = new DataGridViewTextBoxColumn { Name = field, DataPropertyName = field, HeaderText = header.Length == 0 ? field.ToUpperInvariant() : L10n.Get(header), Width = width,
            SortMode = grid.Name is "PlanList" or "PlanAlternatives" ? DataGridViewColumnSortMode.Programmatic : DataGridViewColumnSortMode.NotSortable };
        column.DefaultCellStyle.NullValue = "—";
        if (numeric) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        if (stars) column.CellTemplate = new StarRatingCell();
        grid.Columns.Add(column);
    }
    private static void CopyColumn(DataGridView grid) => grid.Columns.Add(new DataGridViewButtonColumn { Name = "CopyName", HeaderText = "",
        CellTemplate = new CopyNameCell(), Width = 34, Text = L10n.Get("RecruitCopyName"), UseColumnTextForButtonValue = true, SortMode = DataGridViewColumnSortMode.NotSortable });
    private static void InfoColumn(DataGridView grid) => grid.Columns.Add(new DataGridViewTextBoxColumn
    {
        Name = "Info", HeaderText = "i", Width = 30, MinimumWidth = 30, ReadOnly = true,
        SortMode = DataGridViewColumnSortMode.NotSortable,
        DefaultCellStyle = new DataGridViewCellStyle { NullValue = "ⓘ", Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.RoyalBlue }
    });
    public static string InfoText(object? item)
    {
        IEnumerable<string> lines = item switch
        {
            StaffCandidate c => new[] { c.Name, c.CurrentRole + " · " + c.Team, c.InterestLabel + " (" + c.Interest.Score + ")" }
                .Concat(c.Interest.Reasons.Take(3)),
            StaffRenewal r => new[] { r.Name, r.Role + " · " + r.Team, r.Recommendation, r.Explanation },
            _ => []
        };
        // Wrap long paragraphs rather than producing a tooltip wider than the table.
        return string.Join("\n", lines.SelectMany(line => Wrap(line, 85)));
    }
    private static IEnumerable<string> Wrap(string line, int width)
    {
        while (line.Length > width)
        {
            int cut = line.LastIndexOf(' ', width);
            if (cut < 1) cut = width;
            yield return line[..cut]; line = line[cut..].TrimStart();
        }
        if (line.Length > 0) yield return line;
    }
    public void ClearForClub(uint? uid) { clubUid = uid; cached = null; own = null; market = null; choices.Clear(); search.Clear(); RefreshRows(); }
    public void ClearStaff() { cached = null; own = null; choices.Clear(); RefreshRows(); }
    public void ClearMarket() { cached = null; market = null; RefreshRows(); }
    public void SetStaff(StaffResult value) { if (value.ClubUid == clubUid) { cached = null; own = value; RefreshRows(); } }
    public void SetMarket(StaffRecruitmentResult value) { if (value.ClubUid == clubUid) { cached = null; market = value; RefreshRows(); } }
    public void SetLanguage()
    {
        cached = null; L10n.Apply(this);
        int selectedExpiry = expiry.SelectedIndex < 0 ? (int)StaffContractWindow.SixMonths : expiry.SelectedIndex;
        binding = true; expiry.Items.Clear();
        expiry.Items.AddRange(Enum.GetValues<StaffContractWindow>().Select(w => (object)L10n.Get("PlanWindow_" + w)).ToArray());
        expiry.SelectedIndex = selectedExpiry; binding = false;
        tips.SetToolTip(expiry, L10n.Get("PlanExpiryHint"));
        tips.SetToolTip(offer, L10n.Get("RecruitOfferHint"));
        foreach (var grid in new[] { list, alternatives }) ((DataGridViewButtonColumn)grid.Columns["CopyName"]).Text = grid.Columns["CopyName"].ToolTipText = L10n.Get("RecruitCopyName");
        RefreshRows();
    }
    private void RefreshRows()
    {
        if (binding) return;
        tips.Hide(list); tips.Hide(alternatives);
        uint? selected = Selected?.Uid;
        var rows = cached ??= own is null ? [] : StaffPlanning.Build(own, market, (long)offer.Value, choices, (StaffContractWindow)expiry.SelectedIndex);
        var filtered = rows.Where(p => string.IsNullOrWhiteSpace(search.Text) || p.Name.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || p.Role.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || p.Team.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || p.Uid.ToString().Contains(search.Text.Trim()));
        object? Value(StaffRenewal p) => sort switch { "Name" => p.Name, "Role" => p.Role, "Team" => p.Team, "Days" => p.Days,
            "Recommendation" => p.Advice, "Criterion" => p.Criterion, "Quality" => p.Quality, "Stars" => p.Stars, "Wage" => p.Wage, "Ca" => p.Ca, "Pa" => p.Pa,
            "Replacement" => p.Replacement, "ReplacementStars" => p.ReplacementStars, "ReplacementTeam" => p.ReplacementTeam, "BetterCount" => p.BetterCount, _ => p.End };
        var known = filtered.Where(p => Value(p) is not null);
        var sorted = (descending ? known.OrderByDescending(Value) : known.OrderBy(Value)).ThenBy(p => p.Name).ThenBy(p => p.Uid).Concat(filtered.Where(p => Value(p) is null)).ToList();
        binding = true; list.DataSource = new BindingList<StaffRenewal>(sorted); list.ClearSelection();
        foreach (DataGridViewColumn col in list.Columns) col.HeaderCell.SortGlyphDirection = col.Name == sort ? descending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
        if (sorted.Count > 0) list.Rows[Math.Max(0, sorted.FindIndex(p => p.Uid == selected))].Selected = true;
        binding = false;
        status.Text = clubUid is null ? L10n.Get("StaffSelectClub") : own is null ? L10n.Get("PlanPendingStaff") : own.Status != "ready" ? L10n.Format("StaffUnavailable", own.Error ?? "—") :
            StaffPlanning.Date(own.GameDate) is null ? L10n.Get("PlanMissingDate") : L10n.Format("PlanStatus", own.GameDate, sorted.Count, rows.Count) + " " +
            (market is null ? L10n.Get("PlanPendingMarket") : market.Status != "ready" ? L10n.Format("StaffUnavailable", market.Error ?? "—") : market.GameDate != own.GameDate ? L10n.Get("PlanDateMismatch") : L10n.Get("PlanCoverage"));
        Details();
    }
    private void Details()
    {
        var row = Selected; binding = true; criterion.Items.Clear();
        if (row is not null) { criterion.Items.AddRange(StaffPlanning.Profiles(row.Person).Cast<object>().ToArray()); criterion.SelectedIndex = criterion.Items.Cast<StaffSearchProfile>().ToList().FindIndex(p => p.Key == row.Profile?.Key); }
        criterion.Enabled = criterion.Items.Count > 1; findStaff.Enabled = row?.Profile is not null;
        binding = false; RefreshAlternatives(); binding = true;
        int count = alternatives.Rows.Count;
        explanation.Text = row is null ? "" : row.Name + " · " + row.Team + " · " + row.Recommendation + "\n" + row.Explanation + " " + L10n.Format("PlanAlternativeCount", count, row.BetterCount);
        binding = false; AttributeComparison();
    }
    private void RefreshAlternatives()
    {
        var selected = alternatives.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault()?.DataBoundItem as StaffCandidate;
        // Keep the best 50 by score even when sorting another column.
        var candidates = Selected?.Alternatives.OrderByDescending(c => c.Quality).ThenBy(c => c.Name).ThenBy(c => c.Uid).Take(50).ToArray() ?? [];
        object? Value(StaffCandidate c) => alternativeSort switch
        {
            "Name" => c.Name, "Club" => c.Club, "CurrentRole" => c.CurrentRole, "Team" => c.Team,
            "RoleStars" => c.RoleStars, "InterestLabel" => c.Interest.Band, "ClubReputation" => c.ClubReputation,
            "Wage" => c.Wage, "Ca" => c.Ca, "Pa" => c.Pa, "MainAttributes" => c.MainAttributes, _ => c.Quality
        };
        var known = candidates.Where(c => Value(c) is not null);
        var sorted = (alternativeDescending ? known.OrderByDescending(Value) : known.OrderBy(Value))
            .ThenBy(c => c.Name).ThenBy(c => c.Uid).Concat(candidates.Where(c => Value(c) is null)).ToList();
        binding = true; tips.Hide(alternatives);
        alternatives.DataSource = new BindingList<StaffCandidate>(sorted); alternatives.ClearSelection();
        foreach (DataGridViewColumn col in alternatives.Columns)
            col.HeaderCell.SortGlyphDirection = col.Name == alternativeSort ? alternativeDescending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
        if (sorted.Count > 0) alternatives.Rows[Math.Max(0, sorted.FindIndex(c => c.Uid == selected?.Uid))].Selected = true;
        binding = false; AttributeComparison();
    }
    private void AttributeComparison()
    {
        attributes.Rows.Clear();
        if (Selected is not StaffRenewal row) return;
        var candidate = alternatives.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault()?.DataBoundItem as StaffCandidate;
        IEnumerable<string> fields = row.Profile?.Weights.OrderByDescending(w => w.Value).Select(w => w.Key) ?? Enumerable.Empty<string>();
        if (StaffSquads.Matches(row.Person, "subteam")) fields = fields.Prepend("working_with_youngsters");
        foreach (string key in fields.Distinct(StringComparer.Ordinal))
        {
            int? a = StaffRecruitment.Attribute(row.Person, key), b = candidate is null ? null : StaffRecruitment.Attribute(candidate.Person, key);
            int index = attributes.Rows.Add(L10n.Get("StaffAttribute_" + key), a, b, a.HasValue && b.HasValue ? (b.Value - a.Value).ToString("+0;-0;0", L10n.Culture) : "—");
            if (a.HasValue && b.HasValue) attributes.Rows[index].Cells["Delta"].Style.ForeColor = b > a ? Color.DarkGreen : b < a ? Color.Firebrick : SystemColors.ControlText;
        }
        attributes.ClearSelection();
        attributes.Columns["Current"].ToolTipText = row.Name; attributes.Columns["Alternative"].ToolTipText = candidate?.Name ?? "";
    }
    protected override void Dispose(bool disposing) { if (disposing) tips.Dispose(); base.Dispose(disposing); }
}
