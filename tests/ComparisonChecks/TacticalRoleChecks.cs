using FM26SimpleEditor;

internal static class TacticalRoleChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static PlayerDetail Player(uint uid, string name) => new()
    {
        Player = new PlayerRow { Uid = uid, Name = name, TeamId = 10, MainPosition = "STC", Ca = 100, Positions = new() { ["STC"] = 20 } },
        Attributes = TacticalRoles.All.SelectMany(r => r.Focus.Concat(r.Support)).Concat(PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(x => x.Key)).Distinct()
            .Select((key, i) => new EditableField { Key = key, Index = i, Value = 60 }).ToList(),
        Positions = [new EditableField { Key = "STC", Value = 20 }]
    };
    private static void Set(PlayerDetail p, string names, int value)
    {
        foreach (var key in names.Split(' ')) p.Attributes.Single(x => x.Key == key).Value = value;
    }
    public static void Run(Action<bool, string> check)
    {
        check(TacticalRoles.All.Count == 79 && TacticalRoles.All.Count(r => r.Phase == "ip") == 40 && TacticalRoles.All.Count(r => r.Phase == "op") == 39, "embedded catalogue covers all 79 unique role/phase entries");
        check(TacticalRoles.All.Select(r => r.Id).Distinct().Count() == 79 && TacticalRoles.ForPosition("STC").Count() == 10 && TacticalRoles.ForPosition("GK").Count() == 6, "roles are unique and phase-specific options are filtered by position");
        check(!TacticalRoles.ForPosition("SW").Any(), "legacy sweeper position does not get an invented FM26 tactical role");
        foreach (var role in TacticalRoles.All)
        {
            foreach (var pos in role.Positions)
            {
                var weights = TacticalRoles.Weights(new(pos, role.Id));
                check(weights.Count > 0 && weights.Values.All(w => w is >= 1 and <= 3), "transparent role weights " + role.Id + " " + pos);
            }
        }
        var linking = Player(1, "Linking striker"); var target = Player(2, "Target striker");
        linking.Player.Pa = 180;
        Set(linking, "passing vision technique first_touch", 100); Set(linking, "strength heading jumping_reach balance", 40);
        Set(target, "passing vision technique first_touch", 40); Set(target, "strength heading jumping_reach balance", 100);
        var link = new SquadTarget("STC", "ip_deep-lying-forward"); var hold = new SquadTarget("STC", "ip_target-forward");
        var weightsLink = TacticalRoles.Weights(link); var weightsHold = TacticalRoles.Weights(hold);
        check(weightsLink["passing"] == 3 && weightsLink["strength"] == 2 && weightsHold["strength"] == 3 && weightsHold["passing"] == 1, "linking and target striker profiles have genuinely different published editor weights");
        check(SquadSelection.BestForRoles([linking, target], link).First().Player.Uid == 1 && SquadSelection.BestForRoles([linking, target], hold).First().Player.Uid == 2, "different tactical roles prefer different attribute profiles");
        var dual = SquadSelection.BestForRoles([linking, target], link, hold);
        check(dual.Count == 2 && dual[0].PositionScores.Count == 2 && dual[0].Familiarities.Count == 1, "two different roles in the same position retain separate scores without duplicate-key errors");
        check(Math.Abs(dual[0].Score - dual[0].PositionScores.Values.Average()) < 1e-9, "same-position dual-role rating averages both profiles equally");
        check(TacticalRoles.Score(linking, new("STC")) == RoleComparison.Compare(linking, linking, "STC").ScoreA, "generic role preserves the existing position score exactly");
        linking.Player.Ca = 1; linking.Player.Pa = 1;
        check(SquadSelection.BestForRoles([linking, target], link).First().Player.Uid == 1, "CA and PA remain informative and do not change tactical-role suitability");
        linking.Player.Pa = 180;
        bool invalid = false; try { SquadSelection.BestForRoles([linking], new SquadTarget("STC", "op_sweeper-keeper")); } catch (ArgumentException) { invalid = true; }
        check(invalid, "a role incompatible with the chosen position is rejected");
        var missing = Player(3, "Missing data"); missing.Attributes.RemoveAll(f => f.Key == "passing");
        check(TacticalRoles.Score(missing, link) is null && !SquadSelection.BestForRoles([missing], link).Any(), "missing tactical-role attributes do not create a partial recommendation");
        var boosted = Player(4, "Boosted"); foreach (var f in boosted.Attributes) f.Value = 125;
        check(TacticalRoles.Score(boosted, hold) == 20, "tactical scores retain the game-scale clamp for boosted attributes");
        L10n.SetLanguage("en");
        using var host = new Form { Size = new Size(1200, 850) };
        using var panel = new MySquadPanel(); host.Controls.Add(panel);
        panel.SetData(new MySquadResult { Status = "ready", Clubs = [new ManagedSquad { ClubUid = 1, ClubName = "Club", FirstTeamId = 10, TeamIds = [10] }], Players = [linking, target] });
        host.Show(); Application.DoEvents();
        var tabs = All(panel).OfType<TabControl>().Single(); tabs.SelectedIndex = 1; panel.SelectPosition("STC"); Application.DoEvents();
        var choice = All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadRole");
        var secondPosition = All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadSecondPosition");
        var secondRole = All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadSecondRole");
        var ranking = All(panel).OfType<DataGridView>().Single(g => g.Name == "MySquadRanking");
        check(choice.Items.Count == 11 && choice.Items.Cast<object>().Any(x => x.ToString()!.Contains("Out of possession")), "UI offers all striker roles split by phase plus generic fallback");
        choice.SelectedIndex = choice.Items.Cast<object>().ToList().FindIndex(x => x.ToString()!.Contains("Deep-Lying Forward"));
        check(((SquadRecommendation)ranking.Rows[0].Tag!).Player.Uid == 1 && ranking.Columns["Pa"].HeaderText == "PA" && (int)ranking.Rows[0].Cells["Pa"].Value == 180, "role selection recomputes top five and displays potential ability");
        check(ranking.Rows[1].Cells["Pa"].FormattedValue.ToString() == "—" && ranking.Columns["Pa"].DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight, "unknown PA stays unknown and numeric PA is right-aligned");
        secondPosition.SelectedIndex = PositionScheme.Rank("STC") + 1;
        secondRole.SelectedIndex = secondRole.Items.Cast<object>().ToList().FindIndex(x => x.ToString()!.Contains("Target Forward"));
        check(secondRole.Enabled && ((SquadRecommendation)ranking.Rows[0].Tag!).PositionScores.Count == 2, "second selector supports a different role for the same position");
        L10n.SetLanguage("it"); panel.SetLanguage(); Application.DoEvents();
        check(choice.SelectedItem!.ToString()!.Contains("Attaccante di manovra") && secondRole.SelectedItem!.ToString()!.Contains("Fulcro del gioco"), "role IDs and both selected profiles survive Italian language switching");
        using (var form = new TacticalCriteriaForm([link, hold]))
        {
            form.Show(); Application.DoEvents();
            var grid = All(form).OfType<DataGridView>().Single();
            check(grid.ReadOnly && grid.Rows.Count == weightsLink.Count + weightsHold.Count && All(form).OfType<LinkLabel>().Count() == 2, "criteria dialog exposes actual attribute weights and primary source links read-only");
            form.Close();
        }
        panel.SelectPosition("GK");
        check(choice.SelectedIndex == 0 && choice.Items.Count == 7 && !secondRole.Enabled, "changing to incompatible position resets role safely and clears secondary selection");
        host.Close(); L10n.SetLanguage("it");
    }
}
