using System.Globalization;

namespace FM26SimpleEditor;

public sealed class RoleReportPanel : UserControl
{
    private readonly PitchControl pitch = new() { SelectionEnabled = true, AccessibleName = "Scegli la posizione per il report" };
    private readonly ComboBox position = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Posizione del report" };
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
    }

    public void SetContext(PlayerDetail? primary, PlayerDetail? other, bool useItalian)
    {
        string selected = position.SelectedIndex >= 0 ? PositionScheme.Order[position.SelectedIndex] : "";
        if (primaryUid != primary?.Player.Uid) selected = primary is null ? "" : PositionScheme.Main(primary.Positions);
        a = primary; b = other; primaryUid = primary?.Player.Uid; italian = useItalian;
        binding = true;
        position.Items.Clear(); position.Items.AddRange(PositionScheme.Order.Select(key => (object)Labels.Position(key, italian)).ToArray());
        int index = Array.IndexOf(PositionScheme.Order, selected); position.SelectedIndex = index >= 0 ? index : 0;
        position.Enabled = pitch.Enabled = a is not null;
        selector.Text = italian ? "Scegli sul campo" : "Choose on the pitch";
        instruction.Text = italian ? "Clicca una posizione oppure sceglila nell’elenco. Il punto giallo indica la posizione valutata." : "Click a position or use the list. Yellow marks the evaluated position.";
        pitch.SetPositions([], italian); binding = false; Render();
    }

    private void Render()
    {
        table.Rows.Clear(); table.Columns.Clear();
        string key = position.SelectedIndex >= 0 ? PositionScheme.Order[position.SelectedIndex] : "GK";
        pitch.SelectedPosition = key;
        method.Text = italian
            ? "Stima dell’editor per posizione, non per compito tattico: media 1–20 con pesi 3 fondamentale, 2 importante, 1 utile. Familiarità separata; non predice le prestazioni in partita."
            : "Editor position estimate, not a tactical-role rating: 1–20 weighted mean, essential 3, important 2, useful 1. Familiarity is separate; this does not predict match performance.";
        if (a is null || b is null)
        {
            scores.Text = italian ? "Seleziona due giocatori" : "Select two players";
            summary.Text = italian ? "Apri un giocatore e scegli il secondo nel confronto. Il report include le modifiche non salvate del giocatore aperto." : "Open a player and choose a comparison player. The report includes the open player’s unsaved edits.";
            return;
        }
        var result = RoleComparison.Compare(a, b, key);
        var culture = CultureInfo.GetCultureInfo(italian ? "it-IT" : "en-GB");
        string Score(double? value) => value?.ToString("0.00", culture) ?? "—";
        scores.Text = $"{Labels.Position(key, italian)} · {a.Player.Name}: {Score(result.ScoreA)}/20 · {b.Player.Name}: {Score(result.ScoreB)}/20" + (result.Complete ? "" : italian ? " · parziale" : " · partial");
        summary.Text = Describe(result, a.Player.Name, b.Player.Name, italian);
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? "Rilevanza" : "Importance", FillWeight = 17, MinimumWidth = 100 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = italian ? "Attributo" : "Attribute", FillWeight = 31, MinimumWidth = 145 });
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
            row.Cells[3].ToolTipText = stat.Delta > 0 ? a.Player.Name : stat.Delta < 0 ? b.Player.Name : italian ? "Pari" : "Equal";
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
            string text = it ? $"{winner} risulta più forte {aspect.Italian}: {evidence}." : $"{winner} is stronger {aspect.English}: {evidence}.";
            if (counter is not null)
                text += it ? $" {other} conserva però un vantaggio in {Labels.Attribute(counter.Key, it)} (+{Math.Abs(counter.Delta)})."
                    : $" However, {other} retains an advantage in {Labels.Attribute(counter.Key, it)} (+{Math.Abs(counter.Delta)}).";
            sentences.Add(text);
        }
        if (sentences.Count == 0) sentences.Add(it ? "Gli aspetti con almeno due attributi disponibili non mostrano differenze medie di almeno mezzo punto: non emerge uno stile nettamente superiore." : "Aspects with at least two available attributes show no mean gap of half a point or more: no clearly stronger style emerges.");
        string scope = it ? "Lettura delle caratteristiche nella posizione scelta" : "Playing characteristics in the selected position";
        if (!r.Complete) scope += it ? " (analisi parziale)" : " (partial analysis)";
        return scope + ":" + Environment.NewLine + string.Join(Environment.NewLine, sentences) + Environment.NewLine +
            (it ? "Queste differenze descrivono alternative d’impiego: il punteggio complessivo pesa gli attributi del ruolo, mentre la scelta dipende anche dal compito tattico. Velocità e impegno non dimostrano da soli una maggiore propensione offensiva."
                : "These differences describe alternatives for deployment: the overall score weights position attributes, while the choice also depends on tactical duties. Speed and work rate alone do not establish a more attacking instinct.");
    }

    public static string Describe(RoleComparisonResult r, string a, string b, bool it)
    {
        string winner = r.Suggested == ComparisonSide.A ? a : b;
        string conclusion = r.Verdict switch
        {
            RoleVerdict.Incomplete => it ? "Dati incompleti: nessun suggerimento affidabile. Mancano: " + string.Join(", ", r.Missing.Select(k => Labels.Attribute(k, it))) : "Incomplete data: no reliable recommendation. Missing: " + string.Join(", ", r.Missing.Select(k => Labels.Attribute(k, it))),
            RoleVerdict.UnknownFamiliarity => it ? "Familiarità non disponibile: confronta gli attributi, ma non è possibile suggerire chi sia più pronto nella posizione." : "Unknown familiarity: attributes can be compared, but positional readiness cannot be recommended.",
            RoleVerdict.Unfamiliar => it ? "Nessuno dei due raggiunge familiarità 12/20: entrambi richiedono adattamento prima di essere consigliati qui." : "Neither reaches familiarity 12/20: both need adaptation before being recommended here.",
            RoleVerdict.OnlyA or RoleVerdict.OnlyB => it ? $"Preferenza per {winner}: è l’unico con familiarità almeno 12/20. Il vantaggio è di prontezza nella posizione, anche se l’altro può avere attributi superiori." : $"Prefer {winner}: only this player reaches familiarity 12/20. This is a readiness advantage, even if the other player has stronger attributes.",
            RoleVerdict.FamiliarityA or RoleVerdict.FamiliarityB => it ? $"Attributi quasi equivalenti (scarto < 0,25): preferenza per {winner} per familiarità superiore di almeno 3 punti." : $"Nearly equal attributes (gap < 0.25): prefer {winner} for at least 3 points more familiarity.",
            RoleVerdict.AttributesA or RoleVerdict.AttributesB => it ? $"Preferenza sugli attributi per {winner}. Controlla anche la familiarità: il punteggio non applica penalità nascoste per la posizione." : $"Attribute preference: {winner}. Check familiarity too: the score applies no hidden positional penalties.",
            _ => it ? "Profili sostanzialmente equivalenti: nessuna preferenza netta secondo questi criteri." : "Broadly equivalent profiles: no clear preference under these criteria."
        };
        string Familiarity(int? value) => value is null ? it ? "N/D" : "N/A" : value + "/20";
        string Strengths(bool left)
        {
            var fields = r.Stats.Where(s => left ? s.Delta > 0 : s.Delta < 0).OrderByDescending(s => Math.Abs(s.Delta) * s.Weight).ThenBy(s => s.Key, StringComparer.Ordinal).Take(3);
            return string.Join(", ", fields.Select(s => $"{Labels.Attribute(s.Key, it)} (+{Math.Abs(s.Delta)})"));
        }
        string sa = Strengths(true), sb = Strengths(false);
        return conclusion + Environment.NewLine + Environment.NewLine + AnalyzeText(r, a, b, it) + Environment.NewLine + Environment.NewLine + $"{(it ? "Familiarità" : "Familiarity")}: {a} {Familiarity(r.FamiliarityA)} · {b} {Familiarity(r.FamiliarityB)}. " +
            $"{(it ? "Copertura" : "Coverage")}: {r.Stats.Count}/{r.Expected}." + Environment.NewLine +
            $"{a} · {(it ? "vantaggi più rilevanti" : "most relevant advantages")}: {(sa.Length > 0 ? sa : "—")}." + Environment.NewLine +
            $"{b} · {(it ? "vantaggi più rilevanti" : "most relevant advantages")}: {(sb.Length > 0 ? sb : "—")}.";
    }
}

