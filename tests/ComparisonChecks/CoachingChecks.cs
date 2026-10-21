using FM26SimpleEditor;

internal static class CoachingChecks
{
    private static StaffRow Coach(int attack = 8) => new()
    {
        Uid = 1, ClubUid = 1, Name = "Coach",
        Attributes = new()
        {
            ["determination"] = 12, ["discipline"] = 14, ["motivating"] = 16,
            ["coaching_attacking"] = attack, ["coaching_defending"] = 12,
            ["coaching_possession"] = 14, ["coaching_tactical"] = 9,
            ["coaching_technical"] = 15, ["coaching_fitness"] = 17,
            ["coaching_goalkeeping"] = 6, ["coaching_set_pieces"] = 10
        }
    };
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    public static void Run(Action<bool, string> check)
    {
        var person = Coach(); var ratings = CoachingStars.Calculate(person);
        check(ratings.Select(r => r.Score).SequenceEqual(new int?[] { 159, 177, 183, 201, 180, 216, 138, 237, 171 }) &&
            ratings.Select(r => r.Stars).SequenceEqual(new double?[] { 3, 3, 3.5, 3.5, 3.5, 4, 2.5, 4, 3 }),
            "coaching model uses each category and determination, discipline and motivation");
        var specialist = Coach();
        foreach (string key in new[] { "determination", "discipline", "motivating" }) specialist.Attributes[key] = 10;
        specialist.Attributes["coaching_possession"] = 20; specialist.Attributes["coaching_tactical"] = 1; specialist.Attributes["coaching_technical"] = 3;
        var possession = CoachingStars.Calculate(specialist).Where(r => r.Category.Key.StartsWith("Possession")).ToArray();
        check(possession.Select(r => r.Score).SequenceEqual(new int?[] { 126, 138 }) && possession.All(r => r.Stars == 2.5),
            "possession ratings give the primary weight to tactical or technical coaching rather than possession");
        specialist.Attributes["coaching_set_pieces"] = 5; specialist.Attributes["coaching_tactical"] = 20;
        var setPieces = CoachingStars.Calculate(specialist).Single(r => r.Category.Key == "SetPieces");
        check(setPieces.Score == 150 && setPieces.Stars == 3, "set pieces includes tactical coaching as its secondary input");
        specialist.Attributes.Remove("coaching_tactical");
        var incomplete = CoachingStars.Calculate(specialist);
        check(incomplete.Where(r => r.Stars is null).Select(r => r.Category.Key).SequenceEqual(new[] { "AttackTactical", "DefenceTactical", "PossessionTactical", "SetPieces" }),
            "missing tactical coaching blocks only the categories that actually require it, including set pieces");
        (int Score, double Stars)[] boundaries = [(0,.5), (29,.5), (30,1), (59,1), (60,1.5), (89,1.5), (90,2), (119,2),
            (120,2.5), (149,2.5), (150,3), (179,3), (180,3.5), (209,3.5), (210,4), (239,4), (240,4.5), (269,4.5), (270,5), (300,5)];
        check(boundaries.All(b => CoachingStars.FromScore(b.Score) == b.Stars), "half-star bands include both sides of every threshold and the upper cap");
        person.Ca = 200; person.Pa = 200;
        check(CoachingStars.Calculate(person).Select(r => r.Score).SequenceEqual(ratings.Select(r => r.Score)), "CA and PA do not double-count staff coaching attributes");
        person.Attributes.Remove("coaching_attacking");
        var missing = CoachingStars.Calculate(person);
        check(missing.Take(2).All(r => r.Stars is null && r.Missing.Contains("coaching_attacking")) && missing.Skip(2).All(r => r.Stars is not null),
            "missing category input blocks only the affected coaching estimates");
        person.Attributes["discipline"] = 0;
        check(CoachingStars.Calculate(person).All(r => r.Stars is null), "missing or zero DDM blocks every coaching star rather than substituting personality or CA");
        person = Coach(); person.Attributes["motivating"] = 21;
        check(CoachingStars.Calculate(person).All(r => r.Stars is null), "out-of-range inputs do not produce clamped staff estimates");
        person = Coach(); person.IsHumanManager = true;
        check(CoachingStars.Calculate(person).All(r => r.Stars is null), "human manager profiles are not treated as validated non-player layouts");

        person = Coach(); L10n.SetLanguage("it");
        using var host = new Form { Size = new Size(950, 750) }; using var panel = new StaffPanel(); host.Controls.Add(panel);
        panel.ClearForClub(1); panel.SetData(new() { ClubUid = 1, Status = "ready", Staff = [person] });
        host.Show(); Application.DoEvents();
        var list = All(panel).OfType<DataGridView>().Single(g => g.Name == "StaffList"); list.Rows[0].Selected = true;
        var choice = All(panel).OfType<ComboBox>().Single(c => c.Name == "StaffDetailView"); choice.SelectedIndex = 1; Application.DoEvents();
        var stars = All(panel).OfType<DataGridView>().Single(g => g.Name == "StaffCoachingStars");
        check(stars.Visible && stars.Rows.Count == 9 && stars.Rows[0].Cells[1] is StarRatingCell && (double)stars.Rows[0].Cells[1].Value! == 3,
            "staff training view displays estimated category stars with the shared half-star renderer");
        check(stars.ReadOnly && stars.ScrollBars == ScrollBars.Both && stars.Rows[0].Cells[3].Value!.ToString()!.Contains("Determinazione") &&
            All(panel).OfType<Label>().Any(l => l.Text.Contains("formula FM26 non è verificata")), "staff estimates expose inputs and limitations and remain read-only and scrollable");
        L10n.SetLanguage("en"); panel.SetLanguage(); Application.DoEvents();
        check(choice.SelectedIndex == 1 && choice.Text == "Training stars (estimate)" && stars.Rows[0].Cells[0].Value!.ToString() == "Attacking · Tactical" &&
            stars.Rows[0].Cells[3].Value!.ToString()!.Contains("Determination"), "coaching categories, selector and calculation follow language changes");
        person.Attributes.Clear(); panel.SetData(new() { ClubUid = 1, Status = "ready", Staff = [person] });
        check(stars.Rows.Cast<DataGridViewRow>().All(r => r.Cells[1].Value?.ToString() == "—"), "unavailable stars are explicit dashes, never empty golden ratings");
        using (var bitmap = new Bitmap(host.Width, host.Height)) host.DrawToBitmap(bitmap, host.ClientRectangle);
        panel.ClearForClub(null);
        check(stars.Rows.Count == 0, "clearing the club or save removes previous coaching estimates");
        host.Close(); L10n.SetLanguage("it");
    }
}
