using System.ComponentModel;
using System.Reflection;
using FM26SimpleEditor;

internal static class QuickComparisonChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static PlayerDetail Player(uint uid, uint team) => new()
    {
        Player = new PlayerRow { Uid = uid, Name = "Player " + uid, TeamId = team, Team = "Club " + team, MainPosition = "MC", Ca = 100, Pa = 150, Positions = new() { ["MC"] = 20 } },
        RawPa = 150,
        Attributes = PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(f => f.Key).Distinct().Select((key, i) => new EditableField { Index = i, Key = key, Value = 75 }).ToList(),
        Positions = [new EditableField { Key = "MC", Value = 20 }]
    };
    private static void ContextClick(DataGridView grid, int row) => typeof(DataGridView).GetMethod("OnCellMouseDown", Private, null, [typeof(DataGridViewCellMouseEventArgs)], null)!.Invoke(grid,
        [new DataGridViewCellMouseEventArgs(0, row, 3, 3, new MouseEventArgs(MouseButtons.Right, 1, 3, 3, 0))]);

    public static void Run(Action<bool, string> check)
    {
        L10n.SetLanguage("it");
        var players = new[] { Player(1, 10), Player(2, 20), Player(3, 10) };
        using var host = new Form { Size = new Size(1150, 850) };
        using var squad = new MySquadPanel(); host.Controls.Add(squad);
        squad.SetData(new MySquadResult { Status = "ready", Clubs = [new ManagedSquad { ClubUid = 1, FirstTeamId = 10, TeamIds = [10, 20] }], Players = [players[0], players[2]] });
        host.Show(); Application.DoEvents();
        var tabs = All(squad).OfType<TabControl>().Single();
        (uint First, uint Second)? requested = null;
        squad.QuickComparisonRequested += (first, second) => requested = (first, second);
        foreach (string name in new[] { "MySquadRoster", "MySquadRanking" })
        {
            tabs.SelectedIndex = name == "MySquadRoster" ? 0 : 1;
            squad.SelectPosition("MC"); Application.DoEvents();
            var grid = All(squad).OfType<DataGridView>().Single(g => g.Name == name);
            check(grid.MultiSelect && grid.SelectionMode == DataGridViewSelectionMode.FullRowSelect, name + " supports Ctrl row selection");
            grid.ClearSelection(); grid.Rows[1].Selected = true; grid.Rows[0].Selected = true;
            ContextClick(grid, 1);
            check(QuickComparison.TryPair(grid, out var first, out var second) && first == 1 && second == 3, name + " right click preserves both players in visible order");
            var menu = grid.ContextMenuStrip!; menu.Show(grid, new Point(5, 5)); Application.DoEvents();
            var item = menu.Items.OfType<ToolStripMenuItem>().Single(i => i.Name == "QuickComparePair");
            check(item.Enabled, name + " enables the pair command");
            requested = null; item.PerformClick(); menu.Close();
            check(requested == (1u, 3u), name + " emits the exact pair rather than the current cell");
            grid.ClearSelection(); grid.Rows[0].Selected = true;
            check(!QuickComparison.TryPair(grid, out _, out _), name + " refuses a single player");
            L10n.SetLanguage("en"); squad.SetLanguage();
            check(item.Text == "Compare the two selected players", name + " pair command follows language");
            L10n.SetLanguage("it"); squad.SetLanguage();
        }
        host.Hide();
        using var main = new MainForm("python");
        ((System.Windows.Forms.Timer)typeof(MainForm).GetField("debounce", Private)!.GetValue(main)!).Stop();
        var catalog = (List<PlayerRow>)typeof(MainForm).GetField("catalog", Private)!.GetValue(main)!;
        catalog.AddRange(players.Select(p => p.Player));
        var byId = (Dictionary<uint, PlayerRow>)typeof(MainForm).GetField("catalogByUid", Private)!.GetValue(main)!;
        foreach (var p in players) byId.Add(p.Player.Uid, p.Player);
        typeof(MainForm).GetMethod("SetDetail", Private)!.Invoke(main, [players[0]]);
        var compare = All(main).OfType<ComparisonPanel>().Single();
        var cache = (Dictionary<uint, PlayerDetail>)typeof(ComparisonPanel).GetField("cache", Private)!.GetValue(compare)!;
        cache[2] = players[1]; cache[3] = players[2];
        var search = (DataGridView)typeof(MainForm).GetField("players", Private)!.GetValue(main)!;
        search.DataSource = new BindingList<PlayerRow>(catalog); main.Show(); Application.DoEvents();
        var topTabs=(TabControl)typeof(MainForm).GetField("tabs",Private)!.GetValue(main)!;
        var playerTabs=(TabControl)typeof(MainForm).GetField("playerTabs",Private)!.GetValue(main)!;
        check(topTabs.TabPages.Count==3 && playerTabs.TabPages.Count==4 && playerTabs.Parent==topTabs.TabPages[1] &&
            playerTabs.TabPages[0].Text=="Modifica" && playerTabs.TabPages[1].Text=="Anteprima nel gioco" && playerTabs.TabPages[3].Text=="Storico",
            "three main tabs contain four player subtabs including the original edit sheet");
        var showPlayer=typeof(MainForm).GetMethod("ShowPlayerTab",Private)!;
        showPlayer.Invoke(main,[playerTabs.TabPages[1]]);Application.DoEvents();
        check(topTabs.SelectedIndex==1 && playerTabs.SelectedIndex==1 && All(playerTabs.TabPages[1]).OfType<PlayerPreview>().Any(p=>p.Visible),
            "preview shortcut selects the player parent and preview child together");
        typeof(MainForm).GetField("busy",Private)!.SetValue(main,true);
        showPlayer.Invoke(main,[playerTabs.TabPages[3]]);Application.DoEvents();
        check(topTabs.SelectedIndex==1 && playerTabs.SelectedIndex==3 && All(playerTabs.TabPages[3]).OfType<HistoryPanel>().Single().Visible,
            "history is nested under the player sheet rather than left as a main tab");
        topTabs.SelectedIndex=0;typeof(MainForm).GetField("busy",Private)!.SetValue(main,false);
        check(search.MultiSelect, "initial search supports Ctrl row selection");
        search.ClearSelection(); search.Rows[0].Selected = true; search.Rows[1].Selected = true;
        ContextClick(search, 1);
        check(QuickComparison.Selected(search).SequenceEqual(new uint[] { 1, 2 }), "search right click preserves the selected pair");
        var searchMenu = search.ContextMenuStrip!; searchMenu.Show(search, new Point(5, 5)); Application.DoEvents();
        var pairItem = searchMenu.Items.OfType<ToolStripMenuItem>().Single(i => i.Name == "QuickComparePair");
        check(pairItem.Enabled, "search enables pair comparison without using a third player");
        pairItem.PerformClick(); searchMenu.Close(); Application.DoEvents();
        check(ReferenceEquals(typeof(ComparisonPanel).GetField("primary", Private)!.GetValue(compare), players[0]) &&
            ReferenceEquals(typeof(ComparisonPanel).GetField("other", Private)!.GetValue(compare), players[1]), "quick comparison opens both selected players across teams");
        var pool = (HashSet<uint>)typeof(MainForm).GetField("comparisonPool", Private)!.GetValue(main)!;
        check(pool.Contains(1) && pool.Contains(2) && compare.Visible && topTabs.SelectedIndex==1 && playerTabs.SelectedIndex==2, "quick comparison adds both players to pool and opens the comparison tab");
        // Reopening the pair must clear a stale search query and older reference.
        pool.Add(3);
        var query = (TextBox)typeof(ComparisonPanel).GetField("query", Private)!.GetValue(compare)!; query.Text = "not a player";
        typeof(ComparisonPanel).GetMethod("PreferReference")!.Invoke(compare, [3u]);
        check(ReferenceEquals(typeof(ComparisonPanel).GetField("other", Private)!.GetValue(compare), players[2]), "test starts with a different comparison reference");
        ((Task)typeof(MainForm).GetMethod("ComparePair", Private)!.Invoke(main, [1u, 2u])!).GetAwaiter().GetResult(); Application.DoEvents();
        check(query.Text.Length == 0 && ReferenceEquals(typeof(ComparisonPanel).GetField("other", Private)!.GetValue(compare), players[1]), "repeated quick comparison replaces stale reference and filter");
        search.ClearSelection(); foreach (DataGridViewRow row in search.Rows) row.Selected = true;
        check(!QuickComparison.TryPair(search, out _, out _), "three selected players do not silently choose a pair");
        ContextClick(search, 2); check(search.SelectedRows.Count == 3, "context click on selected row does not collapse a larger selection");
        search.ClearSelection(); search.Rows[0].Selected = true; search.Rows[1].Selected = true; ContextClick(search, 2);
        check(QuickComparison.Selected(search).SequenceEqual(new uint[] { 3 }), "context click outside the pair selects only the clicked player");
        ContextClick(search, -1); check(search.SelectedRows.Count == 1, "header context click does not change player selection");
        main.Hide(); L10n.SetLanguage("it");
    }
}
