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
        var top = SquadSelection.BestForPositions(members, "MC");
        check(top.Count == 2 && top[0].Player.Uid == 2 && top[0].Score == 18 && top[1].Player.Uid == 1, "ranking uses position attributes instead of CA; no unsuitable filler");
        check(!top.Any(p => p.Player.Uid is 4 or 5), "ranking excludes inadequate familiarity and incomplete attributes");
        check(SquadSelection.BestForPositions(SquadSelection.Members(data, club, SquadScope.All), "MC").Select(p => p.Player.Uid).SequenceEqual(new uint[] { 3, 2, 6, 1 }), "ranking respects scope and returns the five-candidate limit");
        check(SquadSelection.BestForPositions(members, "GK").Count == 0, "no playable keeper yields an empty recommendation list");
        check(top[0].Score == RoleComparison.Compare(better, better, "MC").ScoreA, "recommendation score matches role comparison exactly");
        var t1 = Player(10, 10, 75, 15); var t2 = Player(11, 10, 75, 20); var t3 = Player(12, 10, 75, 20);
        t2.Player.Name = "Same"; t3.Player.Name = "Same";
        check(SquadSelection.BestForPositions([t3, t1, t2], "MC").Select(p => p.Player.Uid).SequenceEqual(new uint[] { 11, 12, 10 }), "equal scores use familiarity and stable UID tie-breaker");
        bool invalid = false; try { SquadSelection.BestForPositions(members, "bogus"); } catch (ArgumentException) { invalid = true; }
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
        check(roster.Rows.Count == 6 && ranking.Rows.Count == 4, "scope applies to list and ranking together");
        var search = All(panel).OfType<TextBox>().Single(t => t.Name == "MySquadSearch"); search.Text = "1002";
        check(roster.Rows.Count == 1 && ((PlayerRow)roster.Rows[0].DataBoundItem).Uid == 2 && ranking.Rows.Count == 4, "public ID search filters roster without hiding ranking candidates");
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
        StaffChecks(check, panel, tabs);
        panel.ClearForSave(false); Application.DoEvents();
        check(scope.SelectedIndex == 0 && roster.Rows.Count == 0 && ranking.Rows.Count == 0 && All(panel).OfType<Label>().Any(l => l.Text == L10n.Get("MySquadOpen")), "new save clears previous squad and resets default scope");
        panel.SetData(new MySquadResult { Status = "unavailable", Error = "Unknown layout" });
        check(roster.Rows.Count == 0 && !scope.Enabled && All(panel).OfType<Label>().Any(l => l.Text.Contains("Unknown layout")), "optional reader failure has an explicit empty UI state");
        host.Close(); L10n.SetLanguage("it");
    }
    private static void StaffChecks(Action<bool, string> check, MySquadPanel panel, TabControl tabs)
    {
        int requests = 0; panel.StaffRequested += _ => requests++;
        check(requests == 0, "staff is not scanned before opening its sub-tab");
        tabs.SelectedIndex = 2; Application.DoEvents();
        check(requests == 1 && !All(panel).OfType<ComboBox>().Single(c => c.Name == "MySquadScope").Enabled, "staff loads on demand and player-only squad filter is disabled");
        var staff = All(panel).OfType<StaffPanel>().Single();
        var grid = All(staff).OfType<DataGridView>().Single(g => g.Name == "StaffList");
        var details = All(staff).OfType<DataGridView>().Single(g => g.Name == "StaffDetails");
        var a = new StaffRow { Uid = 1, Id = 111, ClubUid = 1, Name = "Alice", ClubName = "Club", ListedClubName = "B team", Age = 40, Wage = 100, Ca = 140, Pa = 160, HasContract = true, JobCode = 20, JobKey = "assistant_manager", Birth = "1980-01-02", Personality = new() { ["professionalism"] = 17 }, Attributes = new() { ["physiotherapy"] = 20, ["judging_potential"] = 15 }, Preferences = new() { ["tempo"] = 18 } };
        var b = new StaffRow { Uid = 2, ClubUid = 1, Name = "Bob", ClubName = "Club", Wage = 0, IsHumanManager = true, HasContract = true };
        var c = new StaffRow { Uid = 3, ClubUid = 1, Name = "Carol", ClubName = "Club" };
        panel.SetStaffData(new StaffResult { Status = "ready", ClubUid = 1, Staff = [a, b, c] });
        panel.EnsureStaffLoaded();
        check(requests == 1 && grid.Rows.Count == 3, "cached staff is not reloaded by returning to the tab");
        check(grid.ReadOnly && details.ReadOnly && grid.ScrollBars == ScrollBars.Both && grid.Columns["Wage"].DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight, "staff tables are read-only with scrolling and numeric alignment");
        check(a.RoleDisplay == "Assistant manager" && b.RoleDisplay == "Human manager" && b.Ca is null && c.RoleDisplay == "Not identified" && c.ContractDisplay == "Not found", "contract job labels coexist with human managers and unavailable jobs");
        var analyst = new StaffRow { JobCode = 60, JobKey = "analyst", JobFamilyOnly = true };
        check(analyst.RoleDisplay == "Analyst (category)" && analyst.RoleTooltip.Length > 0, "a known job category does not claim an exact specialisation");
        check(new StaffRow { JobCode = 3, JobKey = "coach", IsPlayerJob = true }.RoleDisplay == "Player / Coach", "dual player and staff jobs retain both roles");
        check(new StaffRow { JobCode = 144, JobKey = "unsupported" }.RoleDisplay == "Not identified", "unknown job keys never leak untranslated identifiers");
        check(grid.Rows[1].Cells["Ca"].FormattedValue.ToString() == "—" && b.Wage == 0 && a.BirthDisplay == "02/01/1980", "staff nulls differ from real zero pay and dates are displayed correctly");
        grid.Rows[0].Selected = true; Application.DoEvents();
        check(details.Rows.Count == 5 && details.Rows.Cast<DataGridViewRow>().Any(r => r.Cells[1].Value?.ToString() == "Job squad") && details.Rows.Cast<DataGridViewRow>().Any(r => r.Cells[0].Value?.ToString() == "Preferences" && r.Cells[1].Value?.ToString() == "Tempo"), "staff selection separates personality, attributes and preferences");
        check(details.Rows.Cast<DataGridViewRow>().Any(r => r.Cells[1].Value?.ToString() == "Physiotherapy" && r.Cells[2].Value?.ToString() == "20"), "decoded staff attributes have translated labels and 1–20 values");
        staff.SortBy("Wage"); staff.SortBy("Wage");
        check(((StaffRow)grid.Rows[0].DataBoundItem).Uid == 1 && ((StaffRow)grid.Rows[2].DataBoundItem).Uid == 3, "salary sort is numeric with missing values last in descending order");
        var filter = All(staff).OfType<TextBox>().Single(t => t.Name == "StaffSearch"); filter.Text = "111";
        check(grid.Rows.Count == 1 && ((StaffRow)grid.Rows[0].DataBoundItem).Uid == 1, "staff search supports public IDs");
        filter.Text = "assistant";
        check(grid.Rows.Count == 1 && ((StaffRow)grid.Rows[0].DataBoundItem).Uid == 1, "staff search supports decoded job labels");
        filter.Text = "111";
        L10n.SetLanguage("it"); panel.SetLanguage(); Application.DoEvents();
        check(filter.Text == "111" && grid.Columns["RoleDisplay"].HeaderText == "Ruolo" && a.RoleDisplay == "Allenatore in seconda" && analyst.RoleDisplay == "Analista (categoria)" && details.Rows.Cast<DataGridViewRow>().Any(r => r.Cells[0].Value?.ToString() == "Preferenze"), "staff language changes preserve search and translate job labels and detail groups");
        check(details.Rows.Cast<DataGridViewRow>().Any(r => r.Cells[1].Value?.ToString() == "Fisioterapia" && r.Cells[2].Value?.ToString() == "20"), "staff attribute labels switch language without changing values");
        panel.SetStaffData(new StaffResult { ClubUid = 99, Status = "ready" });
        check(grid.Rows.Count == 1, "stale staff response for another club is ignored");
        panel.SetStaffData(new StaffResult { ClubUid = 1, Status = "unavailable", Error = "Staff failure" });
        check(grid.Rows.Count == 0 && details.Rows.Count == 0 && All(staff).OfType<Label>().Any(l => l.Text.Contains("Staff failure")), "staff errors clear only staff results with explicit feedback");
        tabs.SelectedIndex = 0; Application.DoEvents();
        check(All(panel).OfType<DataGridView>().Single(g => g.Name == "MySquadRoster").Rows.Count == 1, "staff reader error preserves player roster and its search");
        L10n.SetLanguage("en"); panel.SetLanguage();
    }

}
