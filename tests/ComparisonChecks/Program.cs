using FM26SimpleEditor;

internal static class Checks
{
    private static int count;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name); count++;
    }
    private static PlayerDetail Player(uint uid, int raw = 75, int familiarity = 20) => new()
    {
        Player = new PlayerRow { Uid = uid, Name = uid == 1 ? "Alpha" : "Beta", Team = "Club", TeamId = 10, Ca = 100, Pa = 150 },
        Attributes = PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(f => f.Key).Distinct().Select((key, i) => new EditableField { Key = key, Index = i, Value = raw }).ToList(),
        Positions = [new EditableField { Key = "MC", Value = familiarity }]
    };
    private static void Set(PlayerDetail p, string key, int value) => p.Attributes.Single(f => f.Key == key).Value = value;
    private static IEnumerable<Control> Descendants(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));

    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            var a = Player(1); var b = Player(2);
            var r = RoleComparison.Compare(a, b, "MC");
            Check(r.Complete && r.ScoreA == 15 && r.ScoreB == 15 && r.Verdict == RoleVerdict.Balanced, "equal profiles");
            Set(a, "passing", 100); r = RoleComparison.Compare(a, b, "MC");
            Check(Math.Abs(r.ScoreA!.Value - 15 - 15.0 / 28) < 1e-9 && r.Suggested == ComparisonSide.A, "essential weight and preference");
            var swapped = RoleComparison.Compare(b, a, "MC");
            Check(swapped.ScoreA == r.ScoreB && swapped.ScoreB == r.ScoreA && swapped.Suggested == ComparisonSide.B, "swap symmetry");
            a = Player(1); b = Player(2); Set(a, "balance", 80);
            Check(RoleComparison.Compare(a, b, "MC").Verdict == RoleVerdict.Balanced, "small useful advantage stays balanced");
            a = Player(1, 76); b = Player(2, 75);
            Check(RoleComparison.Compare(a, b, "MC").Verdict == RoleVerdict.Balanced, "same visible 1-20 values tie");
            a = Player(1, 125); b = Player(2, 100);
            Check(RoleComparison.Compare(a, b, "MC").ScoreA == 20, "boosted attributes clamp at game scale");
            a = Player(1, 50, 15); b = Player(2, 100, 1); r = RoleComparison.Compare(a, b, "MC");
            Check(r.ScoreA == 10 && r.ScoreB == 20 && r.Verdict == RoleVerdict.OnlyA, "readiness choice keeps separate unpenalized scores");
            a = Player(1, 75, 10); b = Player(2, 100, 11); r = RoleComparison.Compare(a, b, "MC");
            Check(r.Verdict == RoleVerdict.Unfamiliar && r.Suggested == ComparisonSide.None, "neither can play: no recommendation");
            a = Player(1, 75, 20); b = Player(2, 75, 15);
            Check(RoleComparison.Compare(a, b, "MC").Verdict == RoleVerdict.FamiliarityA, "familiarity breaks close scores");
            a = Player(1, 75, 20); b = Player(2, 85, 12); r = RoleComparison.Compare(a, b, "MC");
            Check(r.ScoreB == 17 && r.Verdict == RoleVerdict.AttributesB, "both playable: familiarity does not distort attribute scores");
            a.Positions.Clear(); r = RoleComparison.Compare(a, b, "MC");
            Check(r.Verdict == RoleVerdict.UnknownFamiliarity && r.Suggested == ComparisonSide.None, "missing familiarity is not guessed");
            a = Player(1); b = Player(2); a.Attributes.RemoveAll(f => f.Key == "passing"); r = RoleComparison.Compare(a, b, "MC");
            Check(!r.Complete && r.Missing.Contains("passing") && r.Suggested == ComparisonSide.None, "missing attribute blocks recommendation");
            a = Player(1); Set(a, "vision", 0);
            Check(!RoleComparison.Compare(a, b, "MC").Complete, "zero is missing rather than invented 1/20");
            a = Player(1); a.Attributes.Add(new EditableField { Key = "vision", Value = 100 });
            Check(!RoleComparison.Compare(a, b, "MC").Complete, "duplicate attribute blocks recommendation");
            a = Player(1); string original = string.Join(",", a.Attributes.Select(f => f.Value));
            foreach (string key in PositionScheme.Order)
            {
                var fields = RoleImportance.Fields(key).ToArray();
                Check(fields.Length > 0 && fields.Select(f => f.Key).Distinct().Count() == fields.Length && fields.All(f => Labels.Group(f.Key) != "hidden"), "valid shared profile " + key);
                RoleComparison.Compare(a, b, key);
            }
            Check(original == string.Join(",", a.Attributes.Select(f => f.Value)), "report preserves player values");
            bool invalid = false; try { RoleComparison.Compare(a, b, "bad"); } catch (ArgumentException) { invalid = true; }
            Check(invalid, "unknown position rejected");
            Check(PositionScheme.Compact(new Dictionary<string, int>(), true) == "—", "unknown positions stay unknown");
            var positions = new Dictionary<string, int> { ["DL"] = 15, ["DC"] = 20, ["DR"] = 14, ["DM"] = 15, ["MC"] = 18 };
            Check(PositionScheme.Compact(positions, true) == "D (SC), M, C (C)", "Italian multiposition abbreviations");
            Check(PositionScheme.Compact(positions, false) == "D (LC), DM, M (C)", "English multiposition abbreviations");
            Check(PositionScheme.Compact(new Dictionary<string, int> { ["MC"] = 10, ["DM"] = 8 }, true) == "C (C)", "best known position fallback");
            Check(PreviewMath.DeltaText(2, 2) == "← +2" && PreviewMath.DeltaText(-2, -2) == "-2 →", "delta arrow points toward winner");
            Check(PreviewMath.DeltaText(-2, 2) == "← -2" && PreviewMath.DeltaText(2, -2) == "+2 →", "inverse attributes preserve numeric sign");
            Check(PreviewMath.DeltaText(0, 0) == "0 =", "equal delta");
            CommentaryChecks();
            UiChecks();
            LocalizationChecks.Run(Check);
            PlayerToolsChecks.Run(Check);
            HistoryInteractionChecks.Run(Check);
            Console.WriteLine($"{count} C# checks passed"); return 0;
        }
        catch (Exception exc) { Console.Error.WriteLine(exc); return 1; }
    }

    private static void CommentaryChecks()
    {
        var a = Player(1); var b = Player(2);
        a.Positions = [new EditableField { Key = "DR", Value = 20 }];
        b.Positions = [new EditableField { Key = "DR", Value = 18 }];
        (string Key, int A, int B)[] values = [
            ("tackling",16,13), ("anticipation",12,9), ("marking",11,8), ("passing",13,10),
            ("crossing",15,14), ("agility",12,11), ("stamina",13,12), ("pace",9,15),
            ("acceleration",14,15), ("work_rate",8,12), ("teamwork",9,16),
            ("first_touch",11,13), ("dribbling",5,9), ("positioning",9,11), ("decisions",12,12)
        ];
        foreach (var v in values) { Set(a, v.Key, v.A * 5); Set(b, v.Key, v.B * 5); }
        var r = RoleComparison.Compare(a, b, "DR"); var aspects = RoleComparison.Analyze(r);
        Check(r.Suggested == ComparisonSide.B && aspects.Single(x => x.Key == "defence").Delta > .5, "overall preference can differ from defensive preference");
        Check(aspects.Single(x => x.Key == "mobility").Delta < -.5 && aspects.Single(x => x.Key == "support").Delta < -.5, "fullback analysis distinguishes speed and support");
        Check(aspects.All(x => x.Stats.Count >= 2 && x.Stats.All(f => r.Stats.Contains(f))), "analysis uses only shared position attributes with sufficient coverage");
        string text = RoleReportPanel.Describe(r, "Joaquín Buil", "Daniel Hernández", true);
        Check(text.Contains("Joaquín Buil risulta più forte nei duelli") && text.Contains("Daniel Hernández risulta più forte nella mobilità"), "real fullback example yields named playing characteristics");
        Check(text.Contains("Daniel Hernández conserva però un vantaggio in") && text.Contains("Familiarità: Joaquín Buil 20/20 · Daniel Hernández 18/20"), "commentary preserves counterpoints and named familiarity");
        Check(!text.Contains("A ·") && !text.Contains("B ·") && !text.Contains("Familiarità: A "), "report text replaces letter identifiers");
        var swapped = RoleComparison.Analyze(RoleComparison.Compare(b, a, "DR"));
        Check(aspects.All(x => Math.Abs(x.Delta + swapped.Single(y => y.Key == x.Key).Delta) < 1e-9), "aspect analysis is symmetric when swapping players");
        Check(RoleReportPanel.Describe(r, "Buil", "Hernández", false).Contains("Buil is stronger in defensive duels"), "English commentary includes names and evidence");
        a.Attributes.RemoveAll(f => f.Key != "tackling");
        r = RoleComparison.Compare(a, b, "DR");
        Check(RoleComparison.Analyze(r).Count == 0 && RoleReportPanel.Describe(r, "Buil", "Hernández", true).Contains("analisi parziale"), "single observation does not invent a playing profile");
        r = RoleComparison.Compare(Player(1), Player(2), "MC");
        Check(!RoleReportPanel.AnalyzeText(r, "Alpha", "Beta", true).Contains("risulta più forte"), "tied attributes do not invent a style advantage");
    }

    private static void UiChecks()
    {
        var a = Player(1, 100); var b = Player(2);
        using var host = new Form { Size = new Size(1000, 850), StartPosition = FormStartPosition.Manual };
        using var outer = new TabControl { Dock = DockStyle.Fill };
        outer.TabPages.Add(new TabPage("Ricerca")); var comparePage = new TabPage("Confronto"); outer.TabPages.Add(comparePage);
        using var panel = new ComparisonPanel(); comparePage.Controls.Add(panel); host.Controls.Add(outer); host.Show();
        panel.SetContext(a, 100, 150, true, [a.Player, b.Player], new HashSet<uint> { 1, 2 }, true, _ => Task.FromResult(b));
        Application.DoEvents();
        outer.SelectedTab = comparePage; Application.DoEvents();
        var pitches = panel.Controls[0].Controls.Cast<Control>().OfType<TableLayoutPanel>()
            .Single(c => c.Controls.OfType<PitchControl>().Count() == 2);
        Check(pitches.Visible && pitches.Height >= 160 && pitches.Controls.Cast<Control>().All(c => c.Visible && c.Height >= 100), "first entry from hidden parent reserves space for both pitches");
        var grid = Descendants(panel).OfType<DataGridView>().Single(g => g.Columns.Count == 6);
        Check(grid.Columns[0].HeaderText == "Gruppo" && grid.Columns[2].HeaderText.StartsWith("A ·") && grid.Columns[3].HeaderText == "Δ A − B" && grid.Columns[4].HeaderText.StartsWith("B ·"), "comparison column order");
        Check(grid.Rows.Count > 0 && grid.Rows[0].Cells[3].Value!.ToString()!.StartsWith("←"), "rendered winner arrow");
        var views = Descendants(panel).OfType<TabControl>().Single(); views.SelectedIndex = 1; Application.DoEvents();
        var report = Descendants(panel).OfType<RoleReportPanel>().Single();
        Check(report.Visible && report.ClientSize.Height > 240, "report has usable space without duplicate pitch views");
        var reportGrid = Descendants(report).OfType<DataGridView>().Single();
        Check(reportGrid.Columns[2].HeaderText == "Alpha" && reportGrid.Columns[4].HeaderText == "Beta", "report columns use player names");
        Check(Descendants(report).OfType<Label>().Any(l => l.Text.Contains("Alpha: ") && l.Text.Contains("Beta: ")), "report scores use player names");
        views.SelectedIndex = 0; Application.DoEvents();
        Check(pitches.Visible && pitches.Height >= 160, "pitches restored after leaving report");
        views.SelectedIndex = 1; Application.DoEvents();
        var selector = Descendants(report).OfType<ComboBox>().Single(); selector.SelectedIndex = 0; Application.DoEvents();
        Check(Descendants(report).OfType<Label>().Any(l => l.Text.StartsWith("Portiere")), "position picker refreshes report");
        Check(Descendants(report).OfType<DataGridView>().Single().Rows.Count > 0, "report shows relevant keeper attributes");
        host.Size = new Size(900, 560); Application.DoEvents();
        Check(Descendants(report).OfType<DataGridView>().Single().ClientSize.Height >= 50, "short windows retain report table space");
        host.Hide();
        using var main = new MainForm("python");
        foreach (int width in new[] { 900, 1360 })
        {
            main.Size = new Size(width, 800); main.Show(); Application.DoEvents();
            var tabs = Descendants(main).OfType<TabControl>().First();
            var file = Descendants(main).OfType<TextBox>().Single(t => t.ReadOnly && t.Parent is TableLayoutPanel p && p.RowCount == 2 && p.ColumnCount == 4);
            int gap = tabs.PointToScreen(Point.Empty).Y - file.PointToScreen(new Point(0, file.Height)).Y;
            Check(gap >= 0 && gap < 30 * main.DeviceDpi / 96f, "compact global toolbar at width " + width);
        }
        // Resize the real layout surface directly: hosted runners can clamp native windows to a small desktop.
        var mainRoot = main.Controls.OfType<TableLayoutPanel>().Single();
        mainRoot.Dock = DockStyle.None;
        var filters = Descendants(main).OfType<SearchOptions>().Single();
        var cards = filters.Controls.OfType<FlowLayoutPanel>().Single();
        var searchRoot = (TableLayoutPanel)filters.Parent!;
        var resultsGrid = searchRoot.Controls.OfType<DataGridView>().Single();
        var toggle = Descendants(main).OfType<CheckBox>().Single(c => c.Text == "Mostra filtri opzionali");
        int twoRowHeight = 0, oneRowHeight = 0;
        foreach (int width in new[] { 1360, 2600, 1360 })
        {
            mainRoot.Size = new Size(width, 850); Application.DoEvents();
            int rows = cards.Controls.Cast<Control>().Select(c => c.Top).Distinct().Count();
            Check(rows == (width == 2600 ? 1 : 2), "filter wrapping at width " + width);
            int contentBottom = cards.Controls.Cast<Control>().Max(c => c.Bottom + c.Margin.Bottom);
            int gap = resultsGrid.PointToScreen(Point.Empty).Y - cards.PointToScreen(new Point(0, contentBottom)).Y;
            Check(gap >= 0 && gap <= 12 * main.DeviceDpi / 96f, "results follow wrapped filters without empty row at width " + width);
            if (width == 2600) oneRowHeight = filters.Height; else twoRowHeight = filters.Height;
        }
        Check(oneRowHeight < twoRowHeight - 50, "widening releases a full filter row immediately");
        mainRoot.Size = new Size(2600, 850); Application.DoEvents();
        int expandedTop = resultsGrid.Top;
        toggle.Checked = false; Application.DoEvents();
        Check(!filters.Visible && searchRoot.RowStyles[2].Height == 0 && resultsGrid.Top < expandedTop, "hidden filters release all reserved space");
        toggle.Checked = true; Application.DoEvents();
        Check(filters.Visible && filters.Height == oneRowHeight && resultsGrid.Top == expandedTop, "showing filters restores current one-row height");
        mainRoot.Size = new Size(900, 850); Application.DoEvents();
        Check(cards.AutoScroll && cards.VerticalScroll.Visible && filters.Height <= 190 * main.DeviceDpi / 96f + 1, "narrow windows keep extra filter rows scrollable");
    }
}



