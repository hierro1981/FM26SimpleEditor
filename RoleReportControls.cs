using System.Globalization;

namespace FM26SimpleEditor;

public sealed class RoleReportPanel : UserControl
{
    private readonly PitchControl pitch = new() { SelectionEnabled = true, AccessibleName = L10n.Get("Text_86de35bcbb79") };
    private readonly ComboBox position = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = L10n.Get("Text_5533eef0995d") };
    private readonly GroupBox selector = new() { Dock = DockStyle.Fill };
    private readonly Label instruction = new() { AutoSize = true, Dock = DockStyle.Fill, MaximumSize = new Size(220, 0), ForeColor = Color.DimGray };
    private readonly Label scores = new() { AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly TextBox summary = new() { Multiline = true, ReadOnly = true, WordWrap = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = SystemColors.Window };
    private readonly Label method = new() { AutoSize = true, Dock = DockStyle.Fill, ForeColor = Color.DimGray };
    private readonly DataGridView table = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ScrollBars = ScrollBars.Both, BackgroundColor = SystemColors.Window };
    private PlayerDetail? a, b;
    private bool italian = true, binding;
    private uint? primaryUid;

    public RoleReportPanel()
    {
        Dock = DockStyle.Fill;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(4) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.Controls.Add(position, 0, 0); left.Controls.Add(pitch, 0, 1); left.Controls.Add(instruction, 0, 2); selector.Controls.Add(left);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8, 0, 0, 0) };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 155)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.Controls.Add(scores, 0, 0); right.Controls.Add(summary, 0, 1); right.Controls.Add(table, 0, 2); right.Controls.Add(method, 0, 3);
        bool fitting = false;
        void FitSummary()
        {
            if (fitting || right.ClientSize.Height <= 0) return;
            fitting = true;
            try
            {
                int width = Math.Max(100, right.ClientSize.Width - right.Padding.Horizontal - 8);
                scores.MaximumSize = method.MaximumSize = new Size(width, 0);
                int fixedHeight = scores.GetPreferredSize(new Size(width, 0)).Height + method.GetPreferredSize(new Size(width, 0)).Height + 24;
                float scale = DeviceDpi / 96f;
                float height = Math.Clamp((right.ClientSize.Height - fixedHeight) * .38f, 45 * scale, 155 * scale);
                if (Math.Abs(right.RowStyles[1].Height - height) > .5f) right.RowStyles[1].Height = height;
            }
            finally { fitting = false; }
        }
        right.SizeChanged += (_, _) => FitSummary(); scores.TextChanged += (_, _) => FitSummary(); method.TextChanged += (_, _) => FitSummary();
        root.Controls.Add(selector, 0, 0); root.Controls.Add(right, 1, 0); Controls.Add(root);
        pitch.PositionSelected += key => { int index = Array.IndexOf(PositionScheme.Order, key); if (index >= 0) position.SelectedIndex = index; };
        position.SelectedIndexChanged += (_, _) => { if (!binding) Render(); };
        L10n.Capture(this);
    }

    public void SetContext(PlayerDetail? primary, PlayerDetail? other, bool useItalian)
    {
        L10n.Apply(this, useItalian);
        string selected = position.SelectedIndex >= 0 ? PositionScheme.Order[position.SelectedIndex] : "";
        if (primaryUid != primary?.Player.Uid) selected = primary is null ? "" : PositionScheme.Main(primary.Positions);
        a = primary; b = other; primaryUid = primary?.Player.Uid; italian = useItalian;
        binding = true;
        position.Items.Clear(); position.Items.AddRange(PositionScheme.Order.Select(key => (object)Labels.Position(key, italian)).ToArray());
        int index = Array.IndexOf(PositionScheme.Order, selected); position.SelectedIndex = index >= 0 ? index : 0;
        position.Enabled = pitch.Enabled = a is not null;
        selector.Text = italian ? L10n.Get("Text_26b427758acc", true) : L10n.Get("Text_26b427758acc", false);
        instruction.Text = italian ? L10n.Get("Text_596429ce9eb6", true) : L10n.Get("Text_596429ce9eb6", false);
        pitch.SetPositions([], italian); binding = false; Render();
    }

    private void Render()
    {
        table.Rows.Clear(); table.Columns.Clear();
        string key = position.SelectedIndex >= 0 ? PositionScheme.Order[position.SelectedIndex] : "GK";
        pitch.SelectedPosition = key;
        method.Text = italian
            ? L10n.Get("Text_22e221f18045", true)
            : L10n.Get("Text_22e221f18045", false);
        if (a is null || b is null)
        {
            scores.Text = italian ? L10n.Get("Text_018ce93e34b8", true) : L10n.Get("Text_018ce93e34b8", false);
            summary.Text = italian ? L10n.Get("Text_f49050ba3949", true) : L10n.Get("Text_f49050ba3949", false);
            return;
        }
        var result = RoleComparison.Compare(a, b, key);
        var culture = CultureInfo.GetCultureInfo(italian ? L10n.Get("Text_be02ed39e12d", true) : L10n.Get("Text_be02ed39e12d", false));
        string Score(double? value) => value?.ToString("0.00", culture) ?? "—";
        scores.Text = $"{Labels.Position(key, italian)} · {a.Player.Name}: {Score(result.ScoreA)}/20 · {b.Player.Name}: {Score(result.ScoreB)}/20" + (result.Complete ? "" : italian ? L10n.Get("Text_5f7f5f392dd8", true) : L10n.Get("Text_5f7f5f392dd8", false));
        summary.Text = Describe(result, a.Player.Name, b.Player.Name, italian);
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? L10n.Get("Text_c4b0781cd212", true) : L10n.Get("Text_c4b0781cd212", false), FillWeight = 17, MinimumWidth = 100 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? L10n.Get("Text_707eecb875fc", true) : L10n.Get("Text_707eecb875fc", false), FillWeight = 31, MinimumWidth = 145 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = a.Player.Name, FillWeight = 17, MinimumWidth = 70, CellTemplate = new ValueBarCell(20), DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Δ", ToolTipText = a.Player.Name + " − " + b.Player.Name, FillWeight = 18, MinimumWidth = 80, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = b.Player.Name, FillWeight = 17, MinimumWidth = 70, CellTemplate = new ValueBarCell(20), DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        foreach (DataGridViewColumn col in table.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
        table.RowTemplate.Height = 41;
        foreach (var stat in result.Stats.OrderBy(s => s.Tier).ThenBy(s => Labels.Attribute(s.Key, italian), Labels.Comparer(italian)))
        {
            int index = table.Rows.Add(RoleImportance.Name(stat.Tier, italian), Labels.Attribute(stat.Key, italian), stat.A, PreviewMath.DeltaText(stat.Delta, stat.Delta), stat.B);
            var row = table.Rows[index];
            row.Cells[0].Style.ForeColor = RoleImportance.Ink(stat.Tier); row.Cells[0].Style.BackColor = RoleImportance.Back(stat.Tier);
            row.Cells[3].Style.ForeColor = stat.Delta > 0 ? Color.SeaGreen : stat.Delta < 0 ? Color.Firebrick : Color.DimGray;
            row.Cells[3].ToolTipText = stat.Delta > 0 ? a.Player.Name : stat.Delta < 0 ? b.Player.Name : italian ? L10n.Get("Text_1700f4df5398", true) : L10n.Get("Text_1700f4df5398", false);
        }
        table.ClearSelection();
    }

    public static string AnalyzeText(RoleComparisonResult r, string a, string b, bool it)
    {
        var aspects = RoleComparison.Analyze(r);
        var sentences = new List<string>();
        foreach (var aspect in aspects.Where(x => Math.Abs(x.Delta) >= .5))
        {
            bool left = aspect.Delta > 0;
            string winner = left ? a : b, other = left ? b : a;
            string evidence = string.Join(", ", aspect.Stats.Where(s => left ? s.Delta > 0 : s.Delta < 0)
                .OrderByDescending(s => Math.Abs(s.Delta) * s.Weight).Take(3)
                .Select(s => $"{Labels.Attribute(s.Key, it)} (+{Math.Abs(s.Delta)})"));
            var counter = aspect.Stats.Where(s => left ? s.Delta < 0 : s.Delta > 0)
                .OrderByDescending(s => Math.Abs(s.Delta) * s.Weight).FirstOrDefault();
            string text = it ? L10n.FormatIn(true, "Text_de7ca56f56c1", winner, aspect.Italian, evidence) : L10n.FormatIn(false, "Text_de7ca56f56c1", winner, aspect.English, evidence);
            if (counter is not null)
                text += it ? L10n.FormatIn(true, "Text_7b3842e1f4ca", other, Labels.Attribute(counter.Key, it), Math.Abs(counter.Delta))
                    : L10n.FormatIn(false, "Text_7b3842e1f4ca", other, Labels.Attribute(counter.Key, it), Math.Abs(counter.Delta));
            sentences.Add(text);
        }
        if (sentences.Count == 0) sentences.Add(it ? L10n.Get("Text_e17d53cc8e6c", true) : L10n.Get("Text_e17d53cc8e6c", false));
        string scope = it ? L10n.Get("Text_974a57d31063", true) : L10n.Get("Text_974a57d31063", false);
        if (!r.Complete) scope += it ? L10n.Get("Text_3d0e81cec59b", true) : L10n.Get("Text_3d0e81cec59b", false);
        return scope + ":" + Environment.NewLine + string.Join(Environment.NewLine, sentences) + Environment.NewLine +
            (it ? L10n.Get("Text_202c47c07827", true)
                : L10n.Get("Text_202c47c07827", false));
    }

    public static string Describe(RoleComparisonResult r, string a, string b, bool it)
    {
        string winner = r.Suggested == ComparisonSide.A ? a : b;
        string conclusion = r.Verdict switch
        {
            RoleVerdict.Incomplete => it ? L10n.Get("IncompleteData", true) + string.Join(", ", r.Missing.Select(k => Labels.Attribute(k, it))) : L10n.Get("IncompleteData", false) + string.Join(", ", r.Missing.Select(k => Labels.Attribute(k, it))),
            RoleVerdict.UnknownFamiliarity => it ? L10n.Get("Text_1eaee4b23a94", true) : L10n.Get("Text_1eaee4b23a94", false),
            RoleVerdict.Unfamiliar => it ? L10n.Get("Text_d2ff7726ba7d", true) : L10n.Get("Text_d2ff7726ba7d", false),
            RoleVerdict.OnlyA or RoleVerdict.OnlyB => it ? L10n.FormatIn(true, "Text_ff17bc12d7eb", winner) : L10n.FormatIn(false, "Text_ff17bc12d7eb", winner),
            RoleVerdict.FamiliarityA or RoleVerdict.FamiliarityB => it ? L10n.FormatIn(true, "Text_75191df29c9a", winner) : L10n.FormatIn(false, "Text_75191df29c9a", winner),
            RoleVerdict.AttributesA or RoleVerdict.AttributesB => it ? L10n.FormatIn(true, "Text_bd63df3c891a", winner) : L10n.FormatIn(false, "Text_bd63df3c891a", winner),
            _ => it ? L10n.Get("Text_42e01625f528", true) : L10n.Get("Text_42e01625f528", false)
        };
        string Familiarity(int? value) => value is null ? it ? L10n.Get("Text_eb34623b71cd", true) : L10n.Get("Text_eb34623b71cd", false) : value + "/20";
        string Strengths(bool left)
        {
            var fields = r.Stats.Where(s => left ? s.Delta > 0 : s.Delta < 0).OrderByDescending(s => Math.Abs(s.Delta) * s.Weight).ThenBy(s => s.Key, StringComparer.Ordinal).Take(3);
            return string.Join(", ", fields.Select(s => $"{Labels.Attribute(s.Key, it)} (+{Math.Abs(s.Delta)})"));
        }
        string sa = Strengths(true), sb = Strengths(false);
        return conclusion + Environment.NewLine + Environment.NewLine + AnalyzeText(r, a, b, it) + Environment.NewLine + Environment.NewLine + $"{(it ? L10n.Get("Text_d648bc617064", true) : L10n.Get("Text_d648bc617064", false))}: {a} {Familiarity(r.FamiliarityA)} · {b} {Familiarity(r.FamiliarityB)}. " +
            $"{(it ? L10n.Get("Text_4cd0d886b40b", true) : L10n.Get("Text_4cd0d886b40b", false))}: {r.Stats.Count}/{r.Expected}." + Environment.NewLine +
            $"{a} · {(it ? L10n.Get("Text_1d696978d48d", true) : L10n.Get("Text_1d696978d48d", false))}: {(sa.Length > 0 ? sa : "—")}." + Environment.NewLine +
            $"{b} · {(it ? L10n.Get("Text_1d696978d48d", true) : L10n.Get("Text_1d696978d48d", false))}: {(sb.Length > 0 ? sb : "—")}.";
    }
}


