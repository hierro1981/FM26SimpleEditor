using FM26SimpleEditor;

internal static class MySquadEnhancementChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static PlayerDetail Player(uint uid, uint team, string main, int ca, int raw = 75) => new()
    {
        Player = new PlayerRow { Uid = uid, Name = "Player " + uid, TeamId = team, MainPosition = main, Ca = ca, Positions = new() { [main] = 20 } },
        Attributes = PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(f => f.Key).Distinct().Select((key, i) => new EditableField { Index = i, Key = key, Value = raw }).ToList(),
        Positions = [new EditableField { Key = main, Value = 20 }]
    };
    public static void Run(Action<bool, string> check)
    {
        SquadSelection.ResultLimit = 10;
        var mids = Enumerable.Range(1, 7).Select(i => Player((uint)i, 10, "MC", 100 + i)).ToArray();
        foreach (var p in mids) { p.Positions.Add(new EditableField { Key = "DM", Value = 15 }); p.Player.Positions["DM"] = 15; }
        mids[0].Positions.Single(x => x.Key == "DM").Value = 11;
        mids[1].Attributes.RemoveAll(x => x.Key == "marking"); // Not used by MC/DM; full profiles remain complete.
        var many = Enumerable.Range(100, 15).Select(i => Player((uint)i, 10, "MC", 100)).ToArray();
        check(SquadSelection.BestForPositions(many, "MC").Count == 10, "recommendations default to ten");
        SquadSelection.ResultLimit = 3;
        check(SquadSelection.BestForRoles(many, new SquadTarget("MC")).Count == 3, "both ranking entry points use the central limit");
        SquadSelection.ResultLimit = 10;
        var dual = SquadSelection.BestForPositions(mids, "MC", "DM");
        check(dual.Count == 6 && dual.All(p => p.Player.Uid != 1) && dual.All(p => p.Familiarity == 15), "configurable dual-position ranking requires playable familiarity in both");
        var match = dual.First(); var detail = mids.Single(p => p.Player.Uid == match.Player.Uid);
        double expected = (RoleComparison.Compare(detail, detail, "MC").ScoreA!.Value + RoleComparison.Compare(detail, detail, "DM").ScoreA!.Value) / 2;
        check(match.Score == expected && match.PositionScores.Count == 2 && match.Familiarities.Count == 2, "combined score averages both explicit position profiles");
        mids[1].Attributes.RemoveAll(x => x.Key == "strength");
        check(!SquadSelection.BestForPositions(mids, "MC", "DM").Any(p => p.Player.Uid == 2) && SquadSelection.BestForPositions([mids[1]], "MC").Count == 1, "missing attribute in the second profile blocks only the dual-position recommendation");
        check(new SquadRecommendation(detail.Player, 18, 20).Stars == 4.5 && new SquadRecommendation(detail.Player, 20, 20).Stars == 5 && new SquadRecommendation(detail.Player, 15, 20).Stars == 4, "role stars use a fixed five-star scale rounded to half stars");
        bool invalid = false; try { SquadSelection.BestForPositions(mids, "MC", "DM", "DC"); } catch (ArgumentException) { invalid = true; }
        check(invalid, "position recommendations reject more than two positions");
        var keeper = Player(10, 10, "GK", 50); var defender = Player(11, 10, "DC", 190);
        var data = new MySquadResult { Status = "ready", Clubs = [new ManagedSquad { ClubUid = 1, ClubName = "Club", FirstTeamId = 10, TeamIds = [10, 20, 30, 40], Teams = [new ManagedSquadTeam { TeamId = 10, Slot = 0 }, new ManagedSquadTeam { TeamId = 20, Slot = 1, Name = "Club U20" }, new ManagedSquadTeam { TeamId = 30, Slot = 2 }, new ManagedSquadTeam {TeamId=40,Slot=3,Name="Altra squadra",TypeMethod="unclassified_subteam"}] }], Players = [..mids, keeper, defender, Player(20, 20, "MC", 80), Player(30, 30, "MC", 90)] };
        var loan = Player(90, 99, "MC", 150); loan.Player.OnLoan = true; loan.Player.OwnerClubUid = 1;
        var unknownLoan = Player(91, 99, "MC", 150); unknownLoan.Player.OnLoan = true; unknownLoan.Player.OwnerTeam = "Club";
        data.Players.AddRange([loan, unknownLoan]);
        check(!SquadSelection.Members(data, data.Clubs[0], SquadScope.FirstTeam).Any(p => p.Player.Uid == 90) &&
              SquadSelection.Members(data, data.Clubs[0], SquadScope.FirstTeam, includeOutgoingLoans: true).Any(p => p.Player.Uid == 90) &&
              !SquadSelection.Members(data, data.Clubs[0], SquadScope.All, includeOutgoingLoans: true).Any(p => p.Player.Uid == 91), "outgoing loans are opt-in and identified by owner ID, never name");
        check(!SquadSelection.Members(data, data.Clubs[0], SquadScope.OtherTeams, includeOutgoingLoans: true).Any(p => p.Player.Uid == 90) &&
              !SquadSelection.Members(data, data.Clubs[0], SquadScope.All, 20, true).Any(p => p.Player.Uid == 90), "outgoing loans have no guessed youth or reserve assignment");
        check(SquadSelection.Members(data, data.Clubs[0], SquadScope.All, exactTeamId: 20).Single().Player.Uid == 20 && SquadSelection.Members(data, data.Clubs[0], SquadScope.All, exactTeamId: 99).Count == 0, "specific-squad filter is bounded by the managed club's team IDs");
        check(SquadSelection.DefaultOrder([mids[0].Player, mids[1].Player, keeper.Player, defender.Player]).Select(p => p.Uid).SequenceEqual(new uint[] { 10, 11, 2, 1 }), "default order is goalkeeper to striker then descending CA");
        L10n.SetLanguage("it");
        using var host = new Form { Size = new Size(1100, 850) };
        using var panel = new MySquadPanel(); host.Controls.Add(panel); host.Enabled = false;
        panel.SetData(data); host.Enabled = true; host.Show(); Application.DoEvents();
        var scope = All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadScope");
        var roster = All(panel).OfType<DataGridView>().Single(g => g.Name == "MySquadRoster");
        var loans = All(panel).OfType<CheckBox>().Single(c => c.Name == "MySquadIncludeLoans");
        check(!loans.Checked, "outgoing loans are excluded initially");
        loans.Checked = true;
        check(roster.Rows.Cast<DataGridViewRow>().Any(r => ((PlayerRow)r.DataBoundItem).Uid == 90), "my squad loan option updates roster immediately");
        loans.Checked = false;
        check(scope.Enabled && scope.SelectedIndex == 0, "scope becomes enabled immediately after parent loading state ends");
        check(((PlayerRow)roster.Rows[0].DataBoundItem).Uid == 10 && ((PlayerRow)roster.Rows[1].DataBoundItem).Uid == 11 && ((PlayerRow)roster.Rows[2].DataBoundItem).Uid == 7, "initial roster grid uses position and CA ordering");
        check(scope.Items.Count == 6 && scope.Items[3].ToString()!.Contains("Club U20") && scope.Items[4].ToString()!.Contains("slot 2") && scope.Items[4].ToString()!.Contains("30"), "scope shows named and unnamed individual sub-squads without guessed categories");
        scope.SelectedIndex = 3; Application.DoEvents();
        check(roster.Rows.Count == 1 && ((PlayerRow)roster.Rows[0].DataBoundItem).Uid == 20, "individual youth squad is filtered independently");
        L10n.SetLanguage("en"); panel.SetLanguage();
        check(scope.SelectedIndex == 3 && roster.Rows.Count == 1 && scope.Items[3].ToString()!.Contains("Club U20") && scope.Items[5].ToString()!.Contains("Additional squad"), "individual squad choice survives a language change");
        scope.SelectedIndex = 0;
        var tabs = All(panel).OfType<TabControl>().Single(); tabs.SelectedIndex = 1; Application.DoEvents();
        var ranking = All(panel).OfType<DataGridView>().Single(g => g.Name == "MySquadRanking");
        var pitch = All(panel).OfType<PitchControl>().Single();
        pitch.ActivatePosition("MC", false); pitch.ActivatePosition("DM", true); Application.DoEvents();
        check(ranking.Rows.Count == 5 && pitch.SelectedPosition == "MC" && pitch.SecondarySelectedPosition == "DM", "modified pitch click selects two highlighted positions and shows five candidates");
        check(ranking.Columns[4].CellTemplate is StarRatingCell && ranking.Rows[0].Cells[4].Value is double && ranking.Rows[0].Cells[5].Value!.ToString()!.Contains(" · "), "recommendations render role stars and both familiarity values");
        using var bitmap = new Bitmap(ranking.Width, ranking.Height); ranking.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        check(bitmap.Width > 0, "half-star cells render in the live Windows grid");
        pitch.ActivatePosition("DC", true);
        check(pitch.SelectedPosition == "MC" && pitch.SecondarySelectedPosition == "DC", "third modified click replaces the second selection rather than adding a third");
        pitch.ActivatePosition("DC", true);
        check(pitch.SecondarySelectedPosition.Length == 0, "modified click toggles the secondary position off");
        var second = All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadSecondPosition");
        second.SelectedIndex = PositionScheme.Rank("DM") + 1;
        check(pitch.SecondarySelectedPosition == "DM" && ranking.Rows.Count == 5, "second dropdown provides accessible dual-position selection");
        L10n.SetLanguage("it"); panel.SetLanguage();
        check(pitch.SelectedPosition == "MC" && pitch.SecondarySelectedPosition == "DM" && second.SelectedIndex == PositionScheme.Rank("DM") + 1, "dual-position selection survives language changes");
        pitch.ActivatePosition("GK", false);
        check(pitch.SelectedPosition == "GK" && pitch.SecondarySelectedPosition.Length == 0 && ranking.Rows.Count == 1, "plain pitch click returns to a single position without unsuitable filler");
        host.Close(); L10n.SetLanguage("it");
    }
}
