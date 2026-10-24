using FM26SimpleEditor;

internal static class LineupChecks
{
    private static SavedTacticPhase Phase(string position) => new()
    {
        Position = position, Side = "centre", Status = "metadata-mapped",
        CatalogueRoleId = position == "MC" ? "ip_midfield-playmaker" : position == "DM" ? "ip_defensive-midfielder" : "ip_goalkeeper"
    };
    private static SavedTactic Tactic(params string[] positions) => new()
    {
        Slots = positions.Select((p, i) => new SavedTacticSlot { Number = i, InPossession = Phase(p) }).ToList()
    };
    private static PlayerDetail Player(uint uid, int raw, params string[] positions) => new()
    {
        Player = new() { Uid = uid, Name = "P" + uid, Ca = 100, Pa = 100, Age = 17 },
        Attributes = TacticalRoles.All.SelectMany(r => r.Focus.Concat(r.Support)).Concat(PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(f => f.Key))
            .Distinct().Select(k => new EditableField { Key = k, Value = raw }).ToList(),
        Positions = positions.Select(p => new EditableField { Key = p, Value = 20 }).ToList()
    };
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    public static void Run(Action<bool, string> check)
    {
        var tactic = Tactic("MC", "DM");
        var versatile = Player(1, 100, "MC", "DM");
        var midfielder = Player(2, 75, "MC");
        var weakDM = Player(3, 5, "DM");
        var pool = new[] { versatile, midfielder, weakDM };
        var result = LineupSelection.ForTactic(pool, tactic, "ip");
        check(result.Filled == 2 && result.Total == 35 && result.Average == 17.5 &&
            result.Slots[0].Recommendation!.Player.Uid == 2 && result.Slots[1].Recommendation!.Player.Uid == 1,
            "global XI moves the strongest versatile player to cover a weaker position instead of taking greedy slot winners");
        check(LineupSelection.ForTactic(pool.Reverse().Concat([versatile]), tactic, "ip").Slots.Select(s => s.Recommendation?.Player.Uid)
            .SequenceEqual(result.Slots.Select(s => s.Recommendation?.Player.Uid)), "XI is stable under input order and duplicate UIDs cannot fill two slots");
        var incomplete = LineupSelection.ForTactic([versatile], tactic, "ip");
        check(incomplete.Filled == 1 && incomplete.Slots.Count(s => s.Gap == LineupGap.UsedElsewhere) == 1,
            "an undersized squad leaves a visible gap rather than reusing a player");
        check(LineupSelection.ForTactic(pool, tactic, "both").Slots.All(s => s.Gap == LineupGap.UnknownRole),
            "unknown OOP roles block both-phase XI without substituting IP weights");
        var empty = LineupSelection.ForTactic([], Tactic("GK"), "ip");
        check(empty.Filled == 0 && empty.Average is null && empty.Slots[0].Gap == LineupGap.NoCandidate,
            "no eligible goalkeeper yields no fabricated rating or assignment");
        var missing = Player(5, 100, "MC"); missing.Attributes.Clear();
        check(LineupSelection.ForTactic([missing, Player(6, 100, "GK")], Tactic("MC"), "ip").Filled == 0,
            "missing attributes and unfamiliar positions cannot fill XI slots");
        var veteran = Player(10,75,"MC"); veteran.Player.Age=30;
        var prospect = Player(11,60,"MC"); prospect.Player.Ca=80;prospect.Player.Pa=160;
        check(LineupSelection.ForTactic([veteran,prospect],Tactic("MC"),"ip").Slots[0].Recommendation!.Player.Uid==10 &&
            LineupSelection.ForTactic([veteran,prospect],Tactic("MC"),"ip",true) is {Future:true} future && future.Slots[0].Recommendation!.Player.Uid==11,
            "future XI can prefer a developing prospect while the current XI retains today's stronger player");
        var unknown = Player(12,100,"MC");unknown.Player.Pa=null;
        check(LineupSelection.ForTactic([unknown],Tactic("MC"),"ip").Filled==1 &&
            LineupSelection.ForTactic([unknown],Tactic("MC"),"ip",true).Slots[0].Gap==LineupGap.NoCandidate,
            "future XI does not replace unknown potential with today's score or an invented PA");
        var currentProspect=SquadSelection.CandidatesForRoles([prospect],new SquadTarget("MC","ip_midfield-playmaker")).Single();
        var futureProspect=LineupSelection.ForTactic([prospect],Tactic("MC"),"ip",true).Slots[0].Recommendation!;
        check(futureProspect.Player==prospect.Player && futureProspect.Score>currentProspect.Score && currentProspect.Score==12 &&
            prospect.Player.Ca==80 && prospect.Attributes.All(a=>a.Value==60), "future XI carries projected scores without modifying real attributes or ability");
        int oldLimit = SquadSelection.ResultLimit;
        try
        {
            SquadSelection.ResultLimit = 1;
            var many = Enumerable.Range(1, 11).Select(i => Player((uint)i, 75, "MC")).ToArray();
            var full = LineupSelection.ForTactic(many, Tactic(Enumerable.Repeat("MC", 11).ToArray()), "ip");
            check(full.Filled == 11 && full.Slots.Select(s => s.Recommendation!.Player.Uid).Distinct().Count() == 11 && full.Total == 165,
                "XI uses the full eligible pool, independently of the recommendation count setting");
            foreach(var p in many) {p.Player.Ca=80;p.Player.Pa=160;}
            var futureFull=LineupSelection.ForTactic(many,Tactic(Enumerable.Repeat("MC",11).ToArray()),"ip",true);
            check(futureFull.Future && futureFull.Filled==11 && futureFull.Slots.Select(s=>s.Recommendation!.Player.Uid).Distinct().Count()==11,
                "future XI considers the entire projected pool even when the visible shortlist limit is one");
        }
        finally { SquadSelection.ResultLimit = oldLimit; }

        // Independent exhaustive oracle checks arbitrary overlapping eligibility
        // and attribute profiles, including cases with no complete assignment.
        var rng = new Random(219);
        for (int trial = 0; trial < 30; trial++)
        {
            var testTactic = Tactic("MC", "DM", "MC", "GK");
            var people = Enumerable.Range(1, 5).Select(i => Player((uint)i, rng.Next(1, 21) * 5,
                new[] { "MC", "DM", "GK" }.Where(_ => rng.Next(2) == 0).ToArray())).ToArray();
            foreach (var person in people) foreach (var attribute in person.Attributes) attribute.Value = rng.Next(1, 21) * 5;
            foreach (var person in people) {person.Player.Ca=rng.Next(60,140);person.Player.Pa=Math.Min(200,person.Player.Ca+rng.Next(0,80));person.Player.Age=rng.Next(16,35);}
            var scores = testTactic.Slots.Select(s => SquadSelection.CandidatesForRoles(people, TacticSuggestions.Targets(s, "ip")).ToDictionary(r => r.Player.Uid, r => r.Score)).ToArray();
            double Oracle(int slot, HashSet<uint> used)
            {
                if (slot == scores.Length) return 0;
                double best = Oracle(slot + 1, used);
                foreach (var entry in scores[slot])
                    if (used.Add(entry.Key)) { best = Math.Max(best, 1000 + entry.Value + Oracle(slot + 1, used)); used.Remove(entry.Key); }
                return best;
            }
            var optimal = LineupSelection.ForTactic(people, testTactic, "ip");
            check(Math.Abs(optimal.Filled * 1000 + optimal.Total - Oracle(0, [])) < 1e-8, "XI matches exhaustive global optimum " + trial);
            scores=testTactic.Slots.Select(s=>PotentialSelection.CandidatesForRoles(people,TacticSuggestions.Targets(s,"ip"))
                .Where(p=>p.Score.HasValue).ToDictionary(p=>p.Player.Uid,p=>p.Score!.Value)).ToArray();
            var futureOptimal=LineupSelection.ForTactic(people,testTactic,"ip",true);
            check(Math.Abs(futureOptimal.Filled*1000+futureOptimal.Total-Oracle(0,[]))<1e-8,
                "future XI matches exhaustive global optimum without reusing players " + trial);
        }
        L10n.SetLanguage("it");
        using var host = new Form { Size = new Size(1200, 850) };
        using var panel = new TacticsPanel(); host.Controls.Add(panel); panel.ClearForClub(1);
        panel.SetMembers(pool); panel.SetData(new() { ClubUid = 1, Status = "ready", Tactics = [tactic] });
        host.Show(); Application.DoEvents();
        var phase = All(panel).OfType<ComboBox>().Single(c => c.Name == "SavedTacticPhase"); phase.SelectedIndex = 1;
        var pitch = All(panel).OfType<SavedTacticPitch>().Single(c => c.Name == "SavedTacticXI");
        var summary = All(panel).OfType<Label>().Single(c => c.Name == "LineupSummary");
        check(pitch.AccessibleDescription.Contains("P1") && pitch.AccessibleDescription.Contains("P2") && summary.Text.Contains("2/2"),
            "third pitch names optimal players and reports coverage");
        var futureSwitch=All(panel).OfType<CheckBox>().Single(c=>c.Name=="LineupFuture");
        check(!futureSwitch.Checked && futureSwitch.Enabled,"current XI remains the default");
        futureSwitch.Checked=true;
        check(summary.Text.StartsWith("Top 11 futura:") && summary.AccessibleDescription.Contains(L10n.Get("LineupFutureMethod")),
            "future toggle marks the pitch summary and exposes the projection's limitations");
        panel.SetMembers([versatile]);
        check(summary.Text.Contains("1/2") && !pitch.AccessibleDescription.Contains("P2"), "changing squad scope refreshes XI and removes stale names");
        phase.SelectedIndex = 0;
        check(summary.Text.Contains("0/2"), "changing phase recalculates XI eligibility");
        phase.SelectedIndex = 1; L10n.SetLanguage("en"); panel.SetLanguage();
        check(futureSwitch.Checked && summary.Text.StartsWith("Future best XI:") && All(panel).OfType<Label>().Single(l => l.Name == "LineupTitle").Text.Contains("In possession"),
            "XI labels and method change language while preserving the phase and future toggle");
        futureSwitch.Checked=false;
        check(summary.Text.StartsWith("Best XI:"),"switching back restores current XI labels and scores");
        using (var bitmap = new Bitmap(host.Width, host.Height)) host.DrawToBitmap(bitmap, host.ClientRectangle);
        host.Size = new Size(760, 650); Application.DoEvents();
        var viewport = All(panel).OfType<ContentViewport>().Single(c => c.Name == "TacticPitchesScroll");
        check(viewport.HorizontalScroll.Visible, "three pitches remain scrollable when the window is narrow");
        viewport.ScrollControlIntoView(pitch); Application.DoEvents();
        check(viewport.ClientRectangle.IntersectsWith(viewport.RectangleToClient(pitch.RectangleToScreen(pitch.ClientRectangle))), "scrolling reaches the third pitch");
        panel.ClearForClub(null);
        check(pitch.AccessibleDescription == "" && !summary.Visible && !futureSwitch.Checked && !futureSwitch.Enabled, "changing save clears the best XI, mode and summary");
        host.Close(); L10n.SetLanguage("it");
    }
}
