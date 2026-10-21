using FM26SimpleEditor;

internal static class TacticsChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static SavedTacticPhase Phase(string pos, string role) => new() { Position = pos, Side = "centre", CatalogueRoleId = role, Status = "metadata-mapped" };
    private static SavedTactic Tactic(int index) => new()
    {
        Index = index, Name = "Test tactic " + index,
        Slots = Enumerable.Range(0, 11).Select(i => new SavedTacticSlot { Number = i,
            InPossession = Phase(i == 0 ? "MC" : "GK", i == 0 ? "ip_midfield-playmaker" : "ip_goalkeeper"),
            OutOfPossession = Phase(i == 0 ? "DM" : "GK", i == 0 ? "op_defensive-midfielder" : "op_goalkeeper") }).ToList()
    };
    private static PlayerDetail Player(uint uid, uint team) => new()
    {
        Player = new PlayerRow { Uid = uid, Name = "P" + uid.ToString("00"), TeamId = team, Ca = 100, Pa = uid == 2 ? null : 160, Positions = new() { ["MC"] = 20, ["DM"] = uid == 1 ? 11 : 20 } },
        Attributes = TacticalRoles.All.SelectMany(r => r.Focus.Concat(r.Support)).Concat(PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(x => x.Key)).Distinct()
            .Select((key, i) => new EditableField { Key = key, Index = i, Value = uid == 8 ? 100 : 75 }).ToList(),
        Positions = [new EditableField { Key = "MC", Value = 20 }, new EditableField { Key = "DM", Value = uid == 1 ? 11 : 20 }]
    };
    public static void Run(Action<bool, string> check)
    {
        var slot = Tactic(0).Slots[0];
        check(TacticSuggestions.Targets(slot, "both").Select(t => t.RoleId).SequenceEqual(["ip_midfield-playmaker", "op_defensive-midfielder"]), "tactic slot uses its own IP and OOP roles and positions");
        var unknown = new SavedTacticSlot { InPossession = slot.InPossession, OutOfPossession = new() };
        check(TacticSuggestions.Targets(unknown, "both").Length == 0 && TacticSuggestions.Targets(unknown, "op").Length == 0 && TacticSuggestions.Targets(unknown, "ip").Length == 1, "unknown phase never falls back to generic or other-phase recommendations");
        var invalid = new SavedTacticSlot { InPossession = Phase("GK", "ip_midfield-playmaker"), OutOfPossession = slot.OutOfPossession };
        check(TacticSuggestions.Targets(invalid, "ip").Length == 0, "incompatible saved role/position does not produce a suggestion");
        var players = Enumerable.Range(1, 8).Select(i => Player((uint)i, i == 8 ? 20u : 10u)).ToList();
        L10n.SetLanguage("it");
        using var host = new Form { Size = new Size(1200, 900) }; using var squad = new MySquadPanel(); host.Controls.Add(squad);
        squad.SetData(new MySquadResult { Status = "ready", Clubs = [new ManagedSquad { ClubUid = 1, ClubName = "Test", FirstTeamId = 10, TeamIds = [10,20] }], Players = players });
        int requests = 0; squad.TacticsRequested += uid => { if (uid == 1) requests++; };
        host.Show(); Application.DoEvents();
        var tabs = All(squad).OfType<TabControl>().Single(); tabs.SelectedIndex = 3; Application.DoEvents();
        check(requests == 1, "tactics load lazily on first entry");
        squad.SetTacticsData(new SavedTacticsResult { ClubUid = 1, Status = "ready", TeamId = 10, Tactics = [Tactic(0), Tactic(1), Tactic(2)] });
        squad.EnsureTacticsLoaded(); check(requests == 1, "loaded tactic result is cached for its managed club");
        var choice = All(squad).OfType<ComboBox>().Single(c => c.Name == "SavedTacticChoice");
        var phase = All(squad).OfType<ComboBox>().Single(c => c.Name == "SavedTacticPhase");
        var slots = All(squad).OfType<ComboBox>().Single(c => c.Name == "SavedTacticSlot");
        var grid = All(squad).OfType<DataGridView>().Single(g => g.Name == "TacticRanking");
        check(choice.Items.Count == 3 && slots.Items.Count == 11 && All(squad).OfType<SavedTacticPitch>().Count() == 2, "tactics UI exposes three choices, eleven slots, and separate IP/OOP pitches immediately");
        check(grid.Rows.Count == 5 && grid.Rows.Cast<DataGridViewRow>().All(r => ((SquadRecommendation)r.Tag!).Player.Uid != 1 && ((SquadRecommendation)r.Tag!).Player.TeamId == 10), "both-phase top five respects first-team scope and both familiarity thresholds");
        check(grid.Rows[0].Cells[7].FormattedValue.ToString() == "—" && grid.ReadOnly && grid.ScrollBars == ScrollBars.Both, "tactic ranking keeps missing PA, read-only data and both scrollbars");
        phase.SelectedIndex = 1;
        check(((SquadRecommendation)grid.Rows[0].Tag!).Player.Uid == 1 && ((SquadRecommendation)grid.Rows[0].Tag!).PositionScores.Count == 1, "IP-only selection uses just that phase without an OOP familiarity penalty");
        uint comparedA = 0, comparedB = 0;
        squad.QuickComparisonRequested += (a, b) => { comparedA = a; comparedB = b; };
        grid.ClearSelection(); grid.Rows[0].Selected = true; grid.Rows[1].Selected = true;
        var opening = typeof(ContextMenuStrip).GetMethod("OnOpening", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        opening.Invoke(grid.ContextMenuStrip, [new System.ComponentModel.CancelEventArgs()]);
        var compare = grid.ContextMenuStrip!.Items.OfType<ToolStripMenuItem>().Single(i => i.Name == "QuickComparePair");
        compare.PerformClick();
        check(compare.Enabled && comparedA == 1 && comparedB == 2, "tactic ranking forwards a two-player quick comparison in display order");
        phase.SelectedIndex = 0;
        var scope = All(squad).OfType<ComboBox>().Single(c => c.Name == "MySquadScope"); scope.SelectedIndex = 2;
        check(((SquadRecommendation)grid.Rows[0].Tag!).Player.Uid == 8, "tactic recommendations refresh when squad scope changes");
        choice.SelectedIndex = 1;
        L10n.SetLanguage("en"); squad.SetLanguage(); Application.DoEvents();
        check(choice.SelectedIndex == 1 && phase.SelectedItem!.ToString() == "Both phases" && slots.SelectedItem!.ToString()!.Contains("Midfield Playmaker"), "language change preserves selected tactic and phase and translates dynamic roles");
        var panel = All(squad).OfType<TacticsPanel>().Single(); panel.SelectSlot(1); Application.DoEvents();
        check(slots.SelectedIndex == 1 && grid.Rows.Count == 0, "pitch slot selection changes ranking without retaining stale candidates");
        using (var image = new Bitmap(host.Width, host.Height)) host.DrawToBitmap(image, host.ClientRectangle);
        host.Size = new Size(760, 650); Application.DoEvents();
        using (var image = new Bitmap(host.Width, host.Height)) host.DrawToBitmap(image, host.ClientRectangle);
        var pitchesViewport = All(panel).OfType<ContentViewport>().Single(p => p.Name == "TacticPitchesScroll");
        var outerViewport = All(panel).OfType<ContentViewport>().Single(p => p.Name == "TacticsScroll");
        check(pitchesViewport.VerticalScroll.Visible && pitchesViewport.DisplayRectangle.Height >= pitchesViewport.Controls[0].Height,
            "small tactics view actually exposes a scrollbar for the full pitches");
        pitchesViewport.AutoScrollPosition = new Point(0, pitchesViewport.Controls[0].Height); Application.DoEvents();
        var pitch = All(panel).OfType<SavedTacticPitch>().First();
        var keeperPoint = pitchesViewport.PointToClient(pitch.PointToScreen(new Point(pitch.Width / 2, (int)(5 + (pitch.Height - 10) * .92))));
        check(pitchesViewport.ClientRectangle.Contains(keeperPoint), "scrolling reaches goalkeeper row instead of clipping it");
        check(!All(panel).OfType<Label>().Single(l => l.Name == "TacticsStatus").Visible &&
              !All(squad).OfType<Label>().Single(l => l.Name == "MySquadStatus").Visible,
            "ready tactics and squad do not reserve rows for informational paragraphs");
        check(scope.AccessibleDescription == L10n.Get("MySquadTeamNamesHint"), "sub-squad explanation is attached to its selector and follows the language");
        check(tabs is AccentTabControl && tabs.DrawMode == TabDrawMode.OwnerDrawFixed, "squad tab headers use the shared highlighted style");
        host.Size = new Size(520, 520); Application.DoEvents();
        check(outerViewport.VerticalScroll.Visible && outerViewport.HorizontalScroll.Visible, "very small window scrolls the entire tactics surface in both directions");
        var open = All(panel).OfType<Button>().Single(b => b.Text == L10n.Get("Text_9337b648a0e1"));
        outerViewport.ScrollControlIntoView(open); Application.DoEvents();
        check(outerViewport.ClientRectangle.Contains(outerViewport.RectangleToClient(open.RectangleToScreen(open.ClientRectangle))),
            "footer actions remain reachable by scrolling at small sizes");
        host.Size = new Size(1800, 1150); Application.DoEvents();
        Console.WriteLine($"Resize geometry: host={host.Size}, outer={outerViewport.ClientSize}, content={outerViewport.Controls[0].Bounds}, outer bars={outerViewport.HorizontalScroll.Visible}/{outerViewport.VerticalScroll.Visible}, pitches={pitchesViewport.ClientSize}, pitch content={pitchesViewport.Controls[0].Bounds}, pitch bars={pitchesViewport.HorizontalScroll.Visible}/{pitchesViewport.VerticalScroll.Visible}");
        check(!outerViewport.VerticalScroll.Visible && !outerViewport.HorizontalScroll.Visible && !pitchesViewport.VerticalScroll.Visible,
            "growing the window removes unnecessary scrollbars and expands the pitches");
        squad.ClearForSave(true);
        check(choice.Items.Count == 0 && slots.Items.Count == 0 && grid.Rows.Count == 0, "new save clears tactics and all previous recommendations");
        host.Close(); L10n.SetLanguage("it");
    }
}
