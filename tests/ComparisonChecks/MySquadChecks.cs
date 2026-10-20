using FM26SimpleEditor;

internal static class MySquadChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static PlayerDetail Player(uint uid, uint team, int raw, int familiarity, int ca = 100) => new()
    {
        Player = new PlayerRow { Uid = uid, Id = uid + 1000, Name = "Player " + uid, Team = "Club", TeamId = team, MainPosition = "MC", Ca = ca, Positions = new() { ["MC"] = familiarity } },
        Attributes = RoleImportance.Fields("MC").Select((x, i) => new EditableField { Key = x.Key, Index = i, Value = raw }).ToList(),
        Positions = [new EditableField { Key = "MC", Value = familiarity }]
    };
    public static void Run(Action<bool, string> check)
    {
        var first = Player(1, 10, 75, 20, ca: 180);
        var better = Player(2, 10, 90, 12, ca: 80);
        var youth = Player(3, 11, 100, 20);
        var unsuitable = Player(4, 10, 125, 11);
        var missing = Player(5, 10, 125, 20); missing.Attributes.RemoveAt(0);
        var affiliate = Player(6, 30, 85, 15);
        var outside = Player(7, 99, 125, 20);
        var club = new ManagedSquad { ClubUid = 1, ClubName = "Club", ManagerName = "Human", FirstTeamId = 10, TeamIds = [10, 11, 30] };
        var data = new MySquadResult { Status = "ready", Clubs = [club], Players = [first, better, youth, unsuitable, missing, affiliate, outside] };
        var members = SquadSelection.Members(data, club, SquadScope.FirstTeam);
        check(members.Select(p => p.Player.Uid).SequenceEqual(new uint[] { 1, 2, 4, 5 }), "my squad default membership uses first-team ID");
        check(SquadSelection.Members(data, club, SquadScope.OtherTeams).Select(p => p.Player.Uid).SequenceEqual(new uint[] { 3, 6 }), "other squads include affiliated team IDs");
        check(SquadSelection.Members(data, club, SquadScope.All).Count == 6, "all squads exclude outside and outgoing-loan team IDs");
        var top = SquadSelection.TopThree(members, "MC");
        check(top.Count == 2 && top[0].Player.Uid == 2 && top[0].Score == 18 && top[1].Player.Uid == 1, "ranking uses position attributes instead of CA; no unsuitable filler");
        check(!top.Any(p => p.Player.Uid is 4 or 5), "ranking excludes inadequate familiarity and incomplete attributes");
        check(SquadSelection.TopThree(SquadSelection.Members(data, club, SquadScope.All), "MC").Select(p => p.Player.Uid).SequenceEqual(new uint[] { 3, 2, 6 }), "ranking respects scope and returns at most three");
        check(SquadSelection.TopThree(members, "GK").Count == 0, "no playable keeper yields an empty recommendation list");
        check(top[0].Score == RoleComparison.Compare(better, better, "MC").ScoreA, "recommendation score matches role comparison exactly");
        var t1 = Player(10, 10, 75, 15); var t2 = Player(11, 10, 75, 20); var t3 = Player(12, 10, 75, 20);
        t2.Player.Name = "Same"; t3.Player.Name = "Same";
        check(SquadSelection.TopThree([t3, t1, t2], "MC").Select(p => p.Player.Uid).SequenceEqual(new uint[] { 11, 12, 10 }), "equal scores use familiarity and stable UID tie-breaker");
        bool invalid = false; try { SquadSelection.TopThree(members, "bogus"); } catch (ArgumentException) { invalid = true; }
        check(invalid, "unknown recommendation position rejected");
        check(SquadSelection.Members(new MySquadResult(), club, SquadScope.All).Count == 0, "unidentified managed club never returns arbitrary players");
        L10n.SetLanguage("it");
        using var host = new Form { Size = new Size(1100, 800) };
        using var panel = new MySquadPanel(); host.Controls.Add(panel); panel.SetData(data); host.Show(); Application.DoEvents();
        var scope = All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadScope");
        var roster = All(panel).OfType<DataGridView>().Single(g => g.Name == "MySquadRoster");
        var ranking = All(panel).OfType<DataGridView>().Single(g => g.Name == "MySquadRanking");
        check(scope.SelectedIndex == 0 && roster.Rows.Count == 4, "my squad UI defaults to first team");
        check(roster.ScrollBars == ScrollBars.Both && roster.Columns.Cast<DataGridViewColumn>().All(c => c.SortMode == DataGridViewColumnSortMode.Programmatic) && roster.Columns["Age"].DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight, "squad grid retains scrolling sorting and numeric alignment");
        scope.SelectedIndex = 2; Application.DoEvents();
        check(roster.Rows.Count == 6 && ranking.Rows.Count == 3, "scope applies to list and ranking together");
        var search = All(panel).OfType<TextBox>().Single(t => t.Name == "MySquadSearch"); search.Text = "1002";
        check(roster.Rows.Count == 1 && ((PlayerRow)roster.Rows[0].DataBoundItem).Uid == 2 && ranking.Rows.Count == 3, "public ID search filters roster without hiding ranking candidates");
        uint? opened = null; panel.PlayerOpenRequested += uid => opened = uid;
        roster.Rows[0].Selected = true;
        All(panel).OfType<Button>().Where(b => b.Text == L10n.Get("Text_9337b648a0e1")).First().PerformClick();
        check(opened == 2, "roster open action retains exact UID");
        var tabs = All(panel).OfType<TabControl>().Single(); tabs.SelectedIndex = 1; Application.DoEvents();
        panel.SelectPosition("GK"); Application.DoEvents();
        check(ranking.Rows.Count == 0 && All(panel).OfType<PitchControl>().Single().SelectedPosition == "GK", "graphical position selection refreshes recommendations");
        panel.SelectPosition("MC");
        var pitch = All(panel).OfType<PitchControl>().Single();
        foreach (int width in new[] { 900, 1500 })
        {
            panel.Dock = DockStyle.None; panel.Size = new Size(width, 550); Application.DoEvents();
            check(pitch.Visible && pitch.Height >= 350 && ranking.ClientSize.Height > 90 && !pitch.RectangleToScreen(pitch.ClientRectangle).IntersectsWith(ranking.RectangleToScreen(ranking.ClientRectangle)), "pitch and ranking remain separate at width " + width);
        }
        L10n.SetLanguage("en"); panel.SetLanguage(); Application.DoEvents();
        check(scope.SelectedIndex == 2 && scope.Items[0].ToString() == "First team only" && search.Text == "1002" && ranking.Columns[3].HeaderText == "Score /20", "language switch preserves squad scope and search while translating controls");
        check(((ManagedSquad)All(panel).OfType<ComboBox>().Single(c => c.Name == "ManagedClubChoice").SelectedItem).ClubName == "Club", "language switch preserves club and manager names");
        panel.ClearForSave(false); Application.DoEvents();
        check(scope.SelectedIndex == 0 && roster.Rows.Count == 0 && ranking.Rows.Count == 0 && All(panel).OfType<Label>().Any(l => l.Text == L10n.Get("MySquadOpen")), "new save clears previous squad and resets default scope");
        panel.SetData(new MySquadResult { Status = "unavailable", Error = "Unknown layout" });
        check(roster.Rows.Count == 0 && !scope.Enabled && All(panel).OfType<Label>().Any(l => l.Text.Contains("Unknown layout")), "optional reader failure has an explicit empty UI state");
        host.Close(); L10n.SetLanguage("it");
    }
}
