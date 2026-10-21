using System.Drawing.Drawing2D;
using System.Globalization;

namespace FM26SimpleEditor;

public sealed class SavedTacticsResult
{
    public string Status { get; set; } = "none";
    public uint ClubUid { get; set; }
    public uint? TeamId { get; set; }
    public string? Error { get; set; }
    public List<SavedTactic> Tactics { get; set; } = [];
}
public sealed class SavedTactic
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public List<SavedTacticSlot> Slots { get; set; } = [];
    public override string ToString() => $"{Index + 1}. {Name}";
}
public sealed class SavedTacticSlot
{
    public int Number { get; set; }
    public SavedTacticPhase InPossession { get; set; } = new();
    public SavedTacticPhase OutOfPossession { get; set; } = new();
}
public sealed class SavedTacticPhase
{
    public string? Position { get; set; }
    public string? Side { get; set; }
    public string? CatalogueRoleId { get; set; }
    public string Status { get; set; } = "unknown";
    public string Label => CatalogueRoleId is string id && TacticalRoles.All.FirstOrDefault(item => item.Id == id) is TacticalRoleDefinition r
        ? L10n.IsItalian ? r.Italian : r.English : L10n.Get("TacticsUnknownRole");
    public bool CanRecommend(string phase) => Status == "metadata-mapped" && Position is not null && CatalogueRoleId is not null &&
        TacticalRoles.All.Any(r => r.Id == CatalogueRoleId && r.Phase == phase && r.Positions.Contains(Position));
    public string PositionLabel => Position is null ? "—" : Labels.PositionCode(Position, L10n.IsItalian) +
        (Position is "DC" or "DM" or "MC" ? Side == "left" ? L10n.IsItalian ? " (S)" : " (L)" : Side == "right" ? L10n.IsItalian ? " (D)" : " (R)" : " (C)" : "");
}
public static class TacticSuggestions
{
    // Refuse an unknown phase: never silently replace it with generic position weights.
    public static SquadTarget[] Targets(SavedTacticSlot? slot, string phase)
    {
        if (slot is null || phase is not ("ip" or "op" or "both")) return [];
        var selected = new List<SquadTarget>();
        if (phase is "ip" or "both")
        {
            if (!slot.InPossession.CanRecommend("ip")) return [];
            selected.Add(new(slot.InPossession.Position!, slot.InPossession.CatalogueRoleId));
        }
        if (phase is "op" or "both")
        {
            if (!slot.OutOfPossession.CanRecommend("op")) return [];
            selected.Add(new(slot.OutOfPossession.Position!, slot.OutOfPossession.CatalogueRoleId));
        }
        return selected.ToArray();
    }
}

public sealed class SavedTacticPitch : Control
{
    private readonly ToolTip tip = new() { AutoPopDelay = 20000 };
    private readonly Dictionary<int, RectangleF> hits = new();
    private IReadOnlyList<SavedTacticSlot> slots = [];
    private string phase = "ip";
    private int selected = -1;
    public event Action<int>? SlotSelected;
    public SavedTacticPitch() { DoubleBuffered = true; Dock = DockStyle.Fill; MinimumSize = new Size(260, 310); }
    public void SetTactic(IReadOnlyList<SavedTacticSlot> value, string selectedPhase, int selectedSlot)
    { slots = value; phase = selectedPhase; selected = selectedSlot; hits.Clear(); tip.SetToolTip(this, ""); Invalidate(); }
    private SavedTacticPhase Phase(SavedTacticSlot slot) => phase == "ip" ? slot.InPossession : slot.OutOfPossession;
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); hits.Clear();
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var area = new RectangleF(6, 5, Math.Max(1, Width - 12), Math.Max(1, Height - 10));
        using var grass = new SolidBrush(Color.FromArgb(35, 94, 66)); using var line = new Pen(Color.FromArgb(170, 220, 220), 1.2f);
        g.FillRectangle(grass, area); g.DrawRectangle(line, area.X, area.Y, area.Width, area.Height);
        g.DrawLine(line, area.Left, area.Top + area.Height / 2, area.Right, area.Top + area.Height / 2);
        g.DrawEllipse(line, area.Left + area.Width * .38f, area.Top + area.Height * .4f, area.Width * .24f, area.Height * .2f);
        foreach (float y in new[] { area.Top, area.Bottom - area.Height * .13f }) g.DrawRectangle(line, area.Left + area.Width * .27f, y, area.Width * .46f, area.Height * .13f);
        static float Row(SavedTacticPhase p) => p.Position switch { "GK" => .92f, "DC" or "DL" or "DR" => .75f, "DM" or "WBL" or "WBR" => .59f, "MC" or "ML" or "MR" => .43f, "AMC" or "AML" or "AMR" => .26f, _ => .09f };
        static int Column(SavedTacticPhase p) => p.Position?.EndsWith('L') == true ? 0 : p.Position?.EndsWith('R') == true ? 4 : p.Side == "left" ? 1 : p.Side == "right" ? 3 : 2;
        foreach (var slot in slots)
        {
            var p = Phase(slot); if (p.Position is null) continue;
            float y = Row(p);
            var row = slots.Where(s => Phase(s).Position is not null && Row(Phase(s)) == y).OrderBy(s => Column(Phase(s))).ThenBy(s => s.Number).ToList();
            float x = (row.FindIndex(s => s.Number == slot.Number) + .5f) / row.Count;
            float w = Math.Min(area.Width * .30f, area.Width / row.Count * .92f), h = Math.Clamp(area.Height * .12f, 30, 54);
            var box = new RectangleF(area.Left + area.Width * x - w / 2, area.Top + area.Height * y - h / 2, w, h);
            hits[slot.Number] = box;
            using var fill = new SolidBrush(slot.Number == selected ? Color.Goldenrod : Color.FromArgb(236, 244, 240));
            g.FillRectangle(fill, box); g.DrawRectangle(line, box.X, box.Y, box.Width, box.Height);
            TextRenderer.DrawText(g, p.PositionLabel + "\n" + p.Label, Font, Rectangle.Round(box), Color.FromArgb(15, 40, 30), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); var hit = hits.FirstOrDefault(p => p.Value.Contains(e.Location));
        var slot = hits.ContainsKey(hit.Key) && hit.Value.Contains(e.Location) ? slots.FirstOrDefault(s => s.Number == hit.Key) : null;
        Cursor = slot is null ? Cursors.Default : Cursors.Hand;
        string text = slot is null ? "" : Phase(slot).PositionLabel + " · " + Phase(slot).Label;
        if (tip.GetToolTip(this) != text) tip.SetToolTip(this, text);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return;
        foreach (var hit in hits) if (hit.Value.Contains(e.Location)) { SlotSelected?.Invoke(hit.Key); break; }
    }
    protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }
}

public sealed class TacticsPanel : UserControl
{
    private readonly ComboBox choice = new() { Name = "SavedTacticChoice", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox phase = new() { Name = "SavedTacticPhase", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox slotChoice = new() { Name = "SavedTacticSlot", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DropDownWidth = 800 };
    private readonly SavedTacticPitch ip = new() { Name = "SavedTacticIP" }, op = new() { Name = "SavedTacticOP" };
    private readonly Label status = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Label method = new() { AutoSize = true, Dock = DockStyle.Fill, ForeColor = Color.DimGray };
    private readonly DataGridView ranking = new() { Name = "TacticRanking", Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ScrollBars = ScrollBars.Both, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None };
    private readonly ContextMenuStrip menu = new();
    private readonly Button open = new() { AutoSize = true, Text = L10n.Get("Text_9337b648a0e1") };
    private readonly Button criteria = new() { AutoSize = true, Text = L10n.Get("TacticalCriteria") };
    private SavedTacticsResult data = new();
    private IReadOnlyList<PlayerDetail> members = [];
    private uint? clubUid;
    private bool loaded, binding;
    public event Action? ReloadRequested;
    public event Action<uint>? PlayerOpenRequested;
    public event Action<uint>? AddComparisonRequested;
    public event Action<uint, uint>? QuickComparisonRequested;
    private sealed record PhaseChoice(string Key) { public override string ToString() => L10n.Get(Key == "both" ? "TacticsBoth" : Key == "ip" ? "TacticalPhaseIP" : "TacticalPhaseOP"); }
    private sealed record SlotChoice(SavedTacticSlot Slot)
    {
        public override string ToString() => $"{Slot.Number + 1}. {Slot.InPossession.PositionLabel} · {Slot.InPossession.Label} → {Slot.OutOfPossession.PositionLabel} · {Slot.OutOfPossession.Label}";
    }
    private SavedTactic? SelectedTactic => choice.SelectedItem as SavedTactic;
    private SquadTarget[] Targets => TacticSuggestions.Targets((slotChoice.SelectedItem as SlotChoice)?.Slot, (phase.SelectedItem as PhaseChoice)?.Key ?? "both");
    public TacticsPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(6) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var selectors = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
        selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35)); selectors.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var reload = new Button { Name = "TacticsReload", Text = L10n.Get("MySquadReload"), AutoSize = true };
        reload.Click += (_, _) => ReloadRequested?.Invoke();
        selectors.Controls.Add(choice, 0, 0); selectors.Controls.Add(phase, 1, 0); selectors.Controls.Add(reload, 2, 0);
        root.Controls.Add(selectors, 0, 0); root.Controls.Add(status, 0, 1);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var pitches = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, MinimumSize = new Size(560, 340) };
        pitches.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pitches.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pitches.RowStyles.Add(new RowStyle(SizeType.AutoSize)); pitches.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pitches.Controls.Add(new Label { AutoSize = true, Text = L10n.Get("TacticalPhaseIP") }, 0, 0); pitches.Controls.Add(new Label { AutoSize = true, Text = L10n.Get("TacticalPhaseOP") }, 1, 0);
        pitches.Controls.Add(ip, 0, 1); pitches.Controls.Add(op, 1, 1); scroll.Controls.Add(pitches); root.Controls.Add(scroll, 0, 2);
        root.Controls.Add(slotChoice, 0, 3); root.Controls.Add(ranking, 0, 4); root.Controls.Add(method, 0, 5);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; actions.Controls.AddRange([open, criteria]); root.Controls.Add(actions, 0, 6);
        string[] keys = ["#", "Text_f537c6bd038e", "Text_045cb480645d", "MySquadScore", "MySquadStars", "MySquadFamiliarity", "CA", "PA"];
        int[] widths = [40, 200, 125, 85, 110, 140, 60, 60];
        for (int i = 0; i < keys.Length; i++) ranking.Columns.Add(new DataGridViewTextBoxColumn { Name = "TacticColumn" + i, HeaderText = keys[i].Length <= 2 ? keys[i] : L10n.Get(keys[i]), Width = widths[i], SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = i is 0 or >= 3 ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft, NullValue = "—" } });
        ranking.Columns[4].CellTemplate = new StarRatingCell(); ranking.RowTemplate.Height = 41;
        ranking.Columns[3].DefaultCellStyle.Format = "0.00";
        phase.Items.AddRange([new PhaseChoice("both"), new PhaseChoice("ip"), new PhaseChoice("op")]); phase.SelectedIndex = 0;
        choice.SelectedIndexChanged += (_, _) => { if (!binding) FillSlots(); };
        phase.SelectedIndexChanged += (_, _) => { if (!binding) RefreshRanking(); };
        slotChoice.SelectedIndexChanged += (_, _) => { if (!binding) RefreshRanking(); };
        ip.SlotSelected += SelectSlot; op.SlotSelected += SelectSlot;
        open.Click += (_, _) => Open(); ranking.SelectionChanged += (_, _) => open.Enabled = SelectedUid is not null;
        ranking.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Open(); };
        criteria.Click += (_, _) => { if (Targets.Length > 0) { using var dialog = new TacticalCriteriaForm(Targets); dialog.ShowDialog(this); } };
        var compare = new ToolStripMenuItem(L10n.Get("QuickComparePair")) { Name = "QuickComparePair" };
        compare.Click += (_, _) => { if (QuickComparison.TryPair(ranking, out uint a, out uint b)) QuickComparisonRequested?.Invoke(a, b); };
        var add = new ToolStripMenuItem(L10n.Get("Text_885725b27cce")); add.Click += (_, _) => { if (SelectedUid is uint uid) AddComparisonRequested?.Invoke(uid); };
        menu.Items.AddRange([compare, add]); menu.Opening += (sender, e) => { e.Cancel = ranking.SelectedRows.Count == 0; compare.Enabled = QuickComparison.TryPair(ranking, out _, out _); add.Enabled = SelectedUid.HasValue; };
        ranking.ContextMenuStrip = menu; ranking.CellMouseDown += (_, e) => { if (e.Button == MouseButtons.Right) QuickComparison.SelectContextRow(ranking, e.RowIndex, e.ColumnIndex); };
        root.SizeChanged += (_, _) => { status.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 16), 0); method.MaximumSize = status.MaximumSize; };
        Controls.Add(root); L10n.Capture(this); RefreshRanking();
    }
    private uint? SelectedUid => ranking.SelectedRows.Count == 1 ? (ranking.SelectedRows[0].Tag as SquadRecommendation)?.Player.Uid : null;
    private void Open() { if (SelectedUid is uint uid) PlayerOpenRequested?.Invoke(uid); }
    public bool IsLoadedFor(uint uid) => clubUid == uid && loaded;
    public void ClearForClub(uint? uid) { clubUid = uid; loaded = false; data = new(); BindChoices(); }
    public void SetData(SavedTacticsResult result) { if (clubUid != result.ClubUid) return; data = result; loaded = true; BindChoices(); }
    public void SetMembers(IReadOnlyList<PlayerDetail> value) { members = value; RefreshRanking(); }
    public void SetLanguage()
    {
        L10n.Apply(this); string key = (phase.SelectedItem as PhaseChoice)?.Key ?? "both";
        binding = true; phase.Items.Clear(); phase.Items.AddRange([new PhaseChoice("both"), new PhaseChoice("ip"), new PhaseChoice("op")]);
        phase.SelectedIndex = key == "ip" ? 1 : key == "op" ? 2 : 0; binding = false; BindChoices();
    }
    private void BindChoices()
    {
        int previous = SelectedTactic?.Index ?? -1;
        binding = true; choice.Items.Clear(); choice.Items.AddRange(data.Tactics.Cast<object>().ToArray());
        if (choice.Items.Count > 0) choice.SelectedIndex = Math.Max(0, data.Tactics.FindIndex(t => t.Index == previous));
        binding = false; FillSlots();
    }
    private void FillSlots()
    {
        int previous = (slotChoice.SelectedItem as SlotChoice)?.Slot.Number ?? -1;
        binding = true; slotChoice.Items.Clear();
        if (SelectedTactic is SavedTactic tactic) slotChoice.Items.AddRange(tactic.Slots.OrderBy(s => s.Number).Select(s => (object)new SlotChoice(s)).ToArray());
        if (slotChoice.Items.Count > 0) slotChoice.SelectedIndex = Math.Max(0, slotChoice.Items.Cast<SlotChoice>().ToList().FindIndex(s => s.Slot.Number == previous));
        binding = false; RefreshRanking();
    }
    public void SelectSlot(int number)
    {
        int index = slotChoice.Items.Cast<SlotChoice>().ToList().FindIndex(s => s.Slot.Number == number);
        if (index >= 0) slotChoice.SelectedIndex = index;
    }
    private void RefreshRanking()
    {
        var slot = (slotChoice.SelectedItem as SlotChoice)?.Slot;
        ip.SetTactic(SelectedTactic?.Slots ?? [], "ip", slot?.Number ?? -1); op.SetTactic(SelectedTactic?.Slots ?? [], "op", slot?.Number ?? -1);
        status.Text = data.Status == "unavailable" ? L10n.Format("TacticsUnavailable", data.Error ?? "—") : data.Status == "ready" ? L10n.Get("TacticsReadOnly") : L10n.Get("TacticsEmpty");
        var targets = Targets; IReadOnlyList<SquadRecommendation> best = targets.Length == 0 ? [] : SquadSelection.TopFiveForRoles(members, targets);
        ranking.Rows.Clear();
        for (int i = 0; i < best.Count; i++)
        {
            var r = best[i]; int at = ranking.Rows.Add(i + 1, r.Player.Name, PositionScheme.Compact(r.Player.Positions, L10n.IsItalian), r.Score, r.Stars,
                string.Join(" · ", r.Familiarities.Select(f => Labels.PositionCode(f.Key, L10n.IsItalian) + ": " + f.Value)), r.Player.Ca, r.Player.Pa);
            ranking.Rows[at].Tag = r;
            ranking.Rows[at].Cells[3].ToolTipText = string.Join("\n", r.PositionScores.Select(s => TacticalRoles.LabelKey(s.Key) + ": " + s.Value.ToString("0.00", L10n.Culture) + "/20"));
        }
        ranking.Columns[3].DefaultCellStyle.FormatProvider = L10n.Culture;
        ranking.ClearSelection(); open.Enabled = false; criteria.Enabled = targets.Length > 0;
        choice.Enabled = data.Status == "ready" && data.Tactics.Count > 0;
        slotChoice.Enabled = choice.Items.Count > 0; phase.Enabled = slotChoice.Items.Count > 0;
        method.Text = slot is not null && targets.Length == 0 ? L10n.Get("TacticsCannotRank") : L10n.Get("TacticsMethod");
    }
    protected override void Dispose(bool disposing) { if (disposing) menu.Dispose(); base.Dispose(disposing); }
}
