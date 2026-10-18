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
            UiChecks();
            Console.WriteLine($"{count} C# checks passed"); return 0;
        }
        catch (Exception exc) { Console.Error.WriteLine(exc); return 1; }
    }

    private static void UiChecks()
    {
        var a = Player(1, 100); var b = Player(2);
        using var host = new Form { Size = new Size(1000, 850), StartPosition = FormStartPosition.Manual };
        using var panel = new ComparisonPanel(); host.Controls.Add(panel); host.Show();
        panel.SetContext(a, 100, 150, true, [a.Player, b.Player], new HashSet<uint> { 1, 2 }, true, _ => Task.FromResult(b));
        Application.DoEvents();
        var grid = Descendants(panel).OfType<DataGridView>().Single(g => g.Columns.Count == 6);
        Check(grid.Columns[0].HeaderText == "Gruppo" && grid.Columns[2].HeaderText.StartsWith("A ·") && grid.Columns[3].HeaderText == "Δ A − B" && grid.Columns[4].HeaderText.StartsWith("B ·"), "comparison column order");
        Check(grid.Rows.Count > 0 && grid.Rows[0].Cells[3].Value!.ToString()!.StartsWith("←"), "rendered winner arrow");
        var views = Descendants(panel).OfType<TabControl>().Single(); views.SelectedIndex = 1; Application.DoEvents();
        var report = Descendants(panel).OfType<RoleReportPanel>().Single();
        Check(report.Visible && report.ClientSize.Height > 240, "report has usable space without duplicate pitch views");
        var selector = Descendants(report).OfType<ComboBox>().Single(); selector.SelectedIndex = 0; Application.DoEvents();
        Check(Descendants(report).OfType<Label>().Any(l => l.Text.StartsWith("Portiere")), "position picker refreshes report");
        Check(Descendants(report).OfType<DataGridView>().Single().Rows.Count > 0, "report shows relevant keeper attributes");
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
    }
}
