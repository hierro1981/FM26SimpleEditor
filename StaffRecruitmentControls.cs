using System.ComponentModel;

namespace FM26SimpleEditor;

public sealed class StaffRecruitmentPanel : UserControl
{
    private readonly ComboBox mode = new() { Name = "RecruitMode", DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox profile = new() { Name = "RecruitProfile", DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, DropDownWidth = 450 };
    private readonly ComboBox currentJob = new() { Name = "RecruitCurrentJob", DropDownStyle = ComboBoxStyle.DropDownList, Width = 245, DropDownWidth = 360 };
    private sealed record JobChoice(string Key)
    {
        public override string ToString() => L10n.Get(Key.Length == 0 ? "RecruitAnyJob" : Key == "__unknown" ? "StaffUnknownRole" : "StaffJob_" + Key);
    }
    private readonly NumericUpDown offer = new() { Name = "RecruitWage", Minimum = 0, Maximum = 10000000, Increment = 100, ThousandsSeparator = true, Width = 125 };
    private readonly NumericUpDown limit = new() { Name = "RecruitLimit", Minimum = 10, Maximum = 500, Value = 50, Increment = 10, Width = 65 };
    private readonly CheckBox ignoreInterest = new() { Name = "RecruitIgnoreInterest", Text = L10n.Get("RecruitIgnoreInterest"), AutoSize = true };
    private readonly TextBox search = new() { Name = "RecruitSearch", Width = 230, PlaceholderText = L10n.Get("StaffSearch") };
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.DimGray, Dock = DockStyle.Fill };
    private readonly Label reason = new() { Name = "RecruitReasons", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(6) };
    private readonly DataGridView list = Grid("RecruitList");
    private readonly DataGridView attributes = Grid("RecruitAttributes");
    private readonly DataGridView coaching = Grid("RecruitCoaching");
    private readonly ToolTip tips = new() { AutoPopDelay = 30000 };
    private StaffRecruitmentResult? data;
    private uint? clubUid;
    private bool binding;
    private string sort = "Quality";
    private bool descending = true;
    public event Action? ReloadRequested;
    public bool IsLoadedFor(uint id) => clubUid == id && data is not null;
    private StaffSearchProfile? Profile => profile.SelectedItem as StaffSearchProfile;
    public StaffRecruitmentPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(6) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty };
        var reload = new Button { Text = L10n.Get("RecruitLoad"), AutoSize = true };
        reload.Click += (_, _) => ReloadRequested?.Invoke();
        var copy = new Button { Text = L10n.Get("RecruitCopy"), AutoSize = true };
        copy.Click += (_, _) =>
        {
            if (list.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault()?.DataBoundItem is not StaffCandidate c || data is null) return;
            Clipboard.SetText($"FM26 Simple Editor {typeof(StaffRecruitmentPanel).Assembly.GetName().Version} | {data.GameDate}\n" +
                $"{data.ClubName} ({data.ClubUid}) | {c.Name} ({c.Person.Id} / {c.Person.Uid}) | {c.Profile.Label}\n" +
                $"{c.RoleStars} ★ | {c.Quality?.ToString("0.00", L10n.Culture) ?? "—"}/20 | {c.Interest.Label} ({c.Interest.Score})\n" +
                L10n.Format("RecruitFeedbackContext", c.CurrentRole, c.Club, c.Person.ContractEndDisplay, c.Wage?.ToString("N0", L10n.Culture) ?? "—") + "\n" +
                (offer.Value == 0 ? L10n.Get("RecruitNoOffer") : L10n.Get("RecruitOffer") + ": " + offer.Value.ToString("N0", L10n.Culture)) + "\n" +
                c.MainAttributes + "\n" + string.Join("\n", c.Interest.Reasons));
            tips.Show(L10n.Get("RecruitCopied"), copy, 0, copy.Height, 4000);
        };
        var currentJobTools = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        currentJobTools.Controls.AddRange([new Label { Text = L10n.Get("RecruitCurrentRole"), AutoSize = true, Margin = new Padding(6,8,0,0) }, currentJob]);
        tools.Controls.AddRange([mode, profile, currentJobTools, new Label { Text = L10n.Get("RecruitOffer"), AutoSize = true, Margin = new Padding(6,8,0,0) }, offer,
            new Label { Text = L10n.Get("RecruitLimit"), AutoSize = true, Margin = new Padding(6,8,0,0) }, limit, ignoreInterest, search, reload, copy]);
        root.Controls.Add(tools, 0, 0); root.Controls.Add(status, 0, 1); root.Controls.Add(list, 0, 2);
        var lower = new TableLayoutPanel { Name = "RecruitDetailColumns", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); lower.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var left = new GroupBox { Dock = DockStyle.Fill, Text = L10n.Get("RecruitDetail") };
        var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var reasons = new Panel { Name = "RecruitReasonScroll", Dock = DockStyle.Fill, AutoScroll = true };
        reasons.Controls.Add(reason); details.Controls.Add(reasons, 0, 0); details.Controls.Add(attributes, 0, 1); left.Controls.Add(details);
        reasons.SizeChanged += (_, _) => reason.MaximumSize = new Size(Math.Max(80, reasons.ClientSize.Width - 24), 0);
        var right = new GroupBox { Dock = DockStyle.Fill, Text = L10n.Get("StaffCoachingStars") }; right.Controls.Add(coaching);
        lower.Controls.Add(left, 0, 0); lower.Controls.Add(right, 1, 0); root.Controls.Add(lower, 0, 3); Controls.Add(root);
        Add(list, "Name", "Text_f537c6bd038e", 190);
        list.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "CopyName", HeaderText = "", Text = L10n.Get("RecruitCopyName"),
            CellTemplate = new CopyNameCell(), UseColumnTextForButtonValue = true, Width = 34, MinimumWidth = 28, Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable, ToolTipText = L10n.Get("RecruitCopyName")
        });
        Add(list, "CurrentRole", "RecruitCurrentRole", 200);
        Add(list, "JobPreference", "StaffJobPreferenceScore", 185, true);
        Add(list, "RoleStars", "RecruitRoleStars", 180, true, true); Add(list, "Quality", "RecruitQuality", 90, true);
        Add(list, "InterestLabel", "RecruitInterest", 170);
        Add(list, "TrainingCategory", "StaffCoachingCategory", 190);
        Add(list, "MainAttributes", "RecruitAttributes", 650); Add(list, "Club", "StaffClub", 190);
        Add(list, "ClubReputation", "RecruitClubReputation", 165, true);
        Add(list, "CurrentReputation", "StaffPersonalReputation", 180, true);
        Add(list, "Wage", "StaffWage", 140, true); Add(list, "Age", "Text_9efdc62c88bf", 60, true); Add(list, "Ca", "", 60, true);
        Add(list, "PublicId", "Text_358796f71be1", 110, true); Add(list, "Uid", "Text_e1e8b54d45e2", 110, true);
        Add(list, "CurrentRoleStars", "StaffCurrentRoleStars", 225, true, true); Add(list, "TrainingStars", "RecruitBestTraining", 170, true, true);
        list.Columns["JobPreference"].ToolTipText = L10n.Get("StaffJobPreferencesNote");
        list.Columns["Quality"].DefaultCellStyle.Format = "0.00"; list.Columns["Wage"].DefaultCellStyle.Format = "N0";
        list.Columns["CurrentRoleStars"].ToolTipText = L10n.Get("StaffCurrentRoleStarsNote");
        foreach (var grid in new[] { attributes, coaching })
        { Add(grid, "Label", grid == attributes ? "Text_51df0484073a" : "StaffCoachingCategory", 235); Add(grid, "Value", grid == attributes ? "StaffValue" : "StaffCoachingStars", 175, true, grid == coaching); }
        list.RowTemplate.Height = coaching.RowTemplate.Height = 38;
        list.SelectionChanged += (_, _) => Details();
        list.ColumnHeaderMouseClick += (_, e) => { if (list.Columns[e.ColumnIndex].SortMode == DataGridViewColumnSortMode.NotSortable) return; string field = list.Columns[e.ColumnIndex].Name; descending = sort == field && !descending; sort = field; RefreshRows(); };
        list.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && list.Columns[e.ColumnIndex].Name == "CopyName" &&
                list.Rows[e.RowIndex].DataBoundItem is StaffCandidate c && !string.IsNullOrEmpty(c.Name)) Clipboard.SetText(c.Name);
        };
        list.CellToolTipTextNeeded += (_, e) =>
        { if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && list.Rows[e.RowIndex].DataBoundItem is StaffCandidate c)
            e.ToolTipText = list.Columns[e.ColumnIndex].Name == "CopyName" ? L10n.Get("RecruitCopyName") : list.Columns[e.ColumnIndex].Name == "CurrentRoleStars" ? StaffRecruitment.CurrentStarsTooltip(c.Person) : string.Join("\n", c.Interest.Reasons); };
        list.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && list.Rows[e.RowIndex].DataBoundItem is StaffCandidate c && list.Columns[e.ColumnIndex].Name == "InterestLabel")
                e.CellStyle!.ForeColor = c.Interest.Band switch { StaffInterest.Likely => Color.DarkGreen, StaffInterest.Borderline => Color.DarkOrange, StaffInterest.Unlikely => Color.Firebrick, _ => Color.DimGray };
        };
        mode.SelectedIndexChanged += (_, _) => { if (!binding) { Profiles(); RefreshRows(); } };
        profile.SelectedIndexChanged += (_, _) => { if (!binding) { sort = "Quality"; descending = true; RefreshRows(); } };
        currentJob.SelectedIndexChanged += (_, _) => { if (!binding) RefreshRows(); };
        offer.ValueChanged += (_, _) => RefreshRows(); limit.ValueChanged += (_, _) => RefreshRows(); ignoreInterest.CheckedChanged += (_, _) => { sort = "Quality"; descending = true; RefreshRows(); }; search.TextChanged += (_, _) => RefreshRows();
        root.SizeChanged += (_, _) => status.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 16), 0);
        SetLanguage(); L10n.Capture(this);
    }
    private static DataGridView Grid(string name) => new() { Name = name, Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, MultiSelect = false, AllowUserToResizeRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, ScrollBars = ScrollBars.Both, BackgroundColor = SystemColors.Window };
    private static void Add(DataGridView grid, string field, string header, int width, bool numeric = false, bool stars = false)
    {
        var column = new DataGridViewTextBoxColumn { Name = field, DataPropertyName = field, HeaderText = header.Length == 0 ? field.ToUpperInvariant() : L10n.Get(header), Width = width,
            SortMode = grid.Name == "RecruitList" ? DataGridViewColumnSortMode.Programmatic : DataGridViewColumnSortMode.NotSortable };
        column.DefaultCellStyle.NullValue = "—";
        if (numeric) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        if (stars) column.CellTemplate = new StarRatingCell();
        grid.Columns.Add(column);
    }
    private void Profiles()
    {
        var previous = Profile?.Key; binding = true; profile.Items.Clear();
        profile.Items.AddRange((mode.SelectedIndex == 1 ? StaffRecruitment.Training : StaffRecruitment.Roles).Cast<object>().ToArray());
        profile.SelectedIndex = Math.Max(0, profile.Items.Cast<StaffSearchProfile>().ToList().FindIndex(p => p.Key == previous)); binding = false;
    }
    // Set all linked filters together so stale search settings cannot hide the requested job.
    public void ApplyPlanningSearch(StaffSearchProfile selected, string? job, long wage)
    {
        binding = true;
        mode.SelectedIndex = selected.CategoryKey is null ? 0 : 1;
        profile.Items.Clear();
        profile.Items.AddRange((selected.CategoryKey is null ? StaffRecruitment.Roles : StaffRecruitment.Training).Cast<object>().ToArray());
        profile.SelectedIndex = Math.Max(0, profile.Items.Cast<StaffSearchProfile>().ToList().FindIndex(p => p.Key == selected.Key));
        currentJob.SelectedIndex = Math.Max(0, currentJob.Items.Cast<JobChoice>().ToList().FindIndex(c => c.Key == job));
        offer.Value = Math.Clamp((decimal)wage, offer.Minimum, offer.Maximum);
        ignoreInterest.Checked = false; search.Clear(); sort = "Quality"; descending = true;
        binding = false; RefreshRows();
    }
    public void SetLanguage()
    {
        L10n.Apply(this); int selected = Math.Max(0, mode.SelectedIndex); string job = (currentJob.SelectedItem as JobChoice)?.Key ?? ""; binding = true;
        currentJob.Items.Clear(); currentJob.Items.Add(new JobChoice(""));
        currentJob.Items.AddRange(StaffRecruitment.Roles.OrderBy(r => r.Label, StringComparer.CurrentCultureIgnoreCase).Select(r => (object)new JobChoice(r.Key)).ToArray());
        currentJob.Items.Add(new JobChoice("__unknown"));
        currentJob.SelectedIndex = Math.Max(0, currentJob.Items.Cast<JobChoice>().ToList().FindIndex(c => c.Key == job));
        mode.Items.Clear(); mode.Items.AddRange([L10n.Get("RecruitRoles"), L10n.Get("RecruitTraining")]); mode.SelectedIndex = selected; binding = false;
        Profiles(); tips.SetToolTip(offer, L10n.Get("RecruitOfferHint")); tips.SetToolTip(profile, L10n.Get("RecruitQualityHint"));
        list.Columns["JobPreference"].ToolTipText = L10n.Get("StaffJobPreferencesNote");
        list.Columns["RoleStars"].ToolTipText = list.Columns["Quality"].ToolTipText = L10n.Get("RecruitQualityHint");
        ((DataGridViewButtonColumn)list.Columns["CopyName"]).Text = list.Columns["CopyName"].ToolTipText = L10n.Get("RecruitCopyName");
        list.Columns["CurrentRoleStars"].ToolTipText = L10n.Get("StaffCurrentRoleStarsNote"); tips.SetToolTip(ignoreInterest, L10n.Get("RecruitIgnoreInterestHint")); RefreshRows();
    }
    public void ClearForClub(uint? uid) { clubUid = uid; data = null; search.Clear(); RefreshRows(); }
    public void SetData(StaffRecruitmentResult result) { if (result.ClubUid == clubUid) { data = result; RefreshRows(); } }
    private void RefreshRows()
    {
        if (binding) return;
        uint? uid = (list.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault()?.DataBoundItem as StaffCandidate)?.Person.Uid;
        var ranked = data is null || Profile is null ? [] : StaffRecruitment.Rank(data, Profile, (long)offer.Value, ignoreInterest.Checked, search.Text.Trim(), (currentJob.SelectedItem as JobChoice)?.Key);
        object? Value(StaffCandidate c) => sort switch { "CurrentReputation" => c.CurrentReputation, "JobPreference" => c.JobPreference, "ClubReputation" => c.ClubReputation, "PublicId" => c.PublicId, "Uid" => c.Uid, "InterestLabel" => c.Interest.Band == StaffInterest.Likely ? 3 : c.Interest.Band == StaffInterest.Borderline ? 2 : c.Interest.Band == StaffInterest.Uncertain ? 1 : 0,
            "RoleStars" => c.RoleStars, "CurrentRoleStars" => c.CurrentRoleStars, "TrainingStars" => c.TrainingStars, "Wage" => c.Wage, "Age" => c.Age, "Ca" => c.Ca,
            "Name" => c.Name, "CurrentRole" => c.CurrentRole, "Club" => c.Club, "TrainingCategory" => c.TrainingCategory, "MainAttributes" => c.MainAttributes, _ => c.Quality };
        var known = ranked.Where(c => Value(c) is not null);
        var rows = (descending ? known.OrderByDescending(Value) : known.OrderBy(Value)).ThenBy(c => c.Name).ThenBy(c => c.Person.Uid)
            .Concat(ranked.Where(c => Value(c) is null)).Take((int)limit.Value).ToList();
        list.DataSource = new BindingList<StaffCandidate>(rows); list.ClearSelection();
        foreach (DataGridViewColumn column in list.Columns) column.HeaderCell.SortGlyphDirection = column.Name == sort ? descending ? SortOrder.Descending : SortOrder.Ascending : SortOrder.None;
        if (rows.Count > 0) list.Rows[Math.Max(0, rows.FindIndex(c => c.Person.Uid == uid))].Selected = true;
        status.Text = clubUid is null ? L10n.Get("StaffSelectClub") : data is null ? L10n.Get("RecruitPending") : data.Status != "ready" ? L10n.Format("StaffUnavailable", data.Error ?? "—") :
            L10n.Format("RecruitStatus", data.ClubName, rows.Count, ranked.Count, data.PoolPeople) + " " + (ignoreInterest.Checked ? L10n.Get("RecruitInterestOff") + " " : "") + L10n.Get("RecruitCoverage") +
            (data.ReaderWarnings.Count > 0 ? " " + L10n.Get("RecruitReaderWarning") : "");
        Details();
    }
    private void Details()
    {
        attributes.Rows.Clear(); coaching.Rows.Clear(); reason.Text = "";
        if (list.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault()?.DataBoundItem is not StaffCandidate c) return;
        reason.Text = c.Name + " · " + c.Interest.Label + "\n" + string.Join("\n", c.Interest.Reasons);
        if (c.Profile.Key == "doctor") reason.Text += "\n" + L10n.Get("RecruitDoctorMissing");
        foreach (var w in c.Profile.Weights.OrderByDescending(w => w.Value))
            attributes.Rows.Add(L10n.Get("StaffAttribute_" + w.Key), StaffRecruitment.Attribute(c.Person, w.Key)?.ToString(L10n.Culture) ?? "—");
        foreach (var pair in c.Person.Reputation.Where(p => p.Value is >= 1 and <= 10000))
            attributes.Rows.Add(L10n.Get("StaffReputation") + " · " + L10n.Get("StaffReputation_" + pair.Key), pair.Value);
        foreach (var pair in c.Person.JobPreferences.Where(p => p.Value is >= 1 and <= 20 && L10n.Keys.Contains("StaffJob_" + p.Key)))
            attributes.Rows.Add(L10n.Get("StaffJobPreferences") + " · " + L10n.Get("StaffJob_" + pair.Key), pair.Value);
        foreach (var r in CoachingStars.Calculate(c.Person)) coaching.Rows.Add(L10n.Get("CoachingCategory_" + r.Category.Key), (object?)r.Stars);
        foreach (DataGridViewRow row in coaching.Rows) foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = L10n.Get("StaffCoachingEstimate");
        attributes.ClearSelection(); coaching.ClearSelection();
    }
    protected override void Dispose(bool disposing) { if (disposing) tips.Dispose(); base.Dispose(disposing); }
}
