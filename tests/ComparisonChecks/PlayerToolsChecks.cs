using FM26SimpleEditor;

internal static class PlayerToolsChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(All(x)));
    private static HistoryResult History(int count, string date) => new()
    {
        EmbeddedAvailable = true,
        EmbeddedPoints = Enumerable.Range(1, count).Select(i => new HistoryPoint { Sequence = i, Source = "save.fm", Attributes = new() { ["dribbling"] = i is 3 or 4 ? 40 : 45, ["technique"] = 60 } }).ToList(),
        Points = [new HistoryPoint { Date = date, Source = "save.fm", Attributes = new() { ["dribbling"] = 55, ["technique"] = 65 } }]
    };
    public static void Run(Action<bool, string> check)
    {
        L10n.SetLanguage("it");
        var history = History(19, "2028-02-10");
        var months = HistoryCalendar.ReconstructMonths(history);
        check(months.Count == 19 && months[1] == new DateTime(2026, 7, 1) && months[19] == new DateTime(2028, 1, 1), "screenshot history maps to July 2026 through January 2028");
        var older = HistoryCalendar.ReconstructMonths(History(14, "2027-09-16"));
        var newer = HistoryCalendar.ReconstructMonths(History(16, "2027-11-07"));
        check(older[1] == months[1] && newer[1] == months[1] && older.All(p => newer[p.Key] == p.Value), "two actual save dates reconstruct a stable monthly origin");
        check(history.EmbeddedPoints.All(p => p.Date == "") && history.Points[0].Date == "2028-02-10", "monthly projection never overwrites actual or undecoded dates");
        var leap = HistoryCalendar.ReconstructMonths(History(2, "2028-03-31"));
        check(leap[1] == new DateTime(2028, 1, 1) && leap[2] == new DateTime(2028, 2, 1), "calendar reconstruction handles leap February and month-end saves");
        var invalid = History(3, "bad");
        check(HistoryCalendar.ReconstructMonths(invalid).Count == 0, "missing save date keeps ordinal history");
        invalid = History(3, "2028-02-10"); invalid.EmbeddedPoints[2].Sequence = 2;
        check(HistoryCalendar.ReconstructMonths(invalid).Count == 0, "duplicate or missing sequence numbers do not invent months");
        invalid = History(3, "2028-02-10"); invalid.EmbeddedPoints[0].Source = "other.fm";
        check(HistoryCalendar.ReconstructMonths(invalid).Count == 0, "mixed save sources do not receive a calendar");
        invalid = History(3, "2028-02-10"); invalid.Points.Add(new HistoryPoint { Source = "save.fm", Date = "2028-03-10" });
        check(HistoryCalendar.ReconstructMonths(invalid).Count == 0, "ambiguous reference snapshots keep ordinal history");
        invalid = History(3, "2028-02-10"); invalid.EmbeddedPoints[0].Date = "2027-11-15";
        check(HistoryCalendar.ReconstructMonths(invalid).Count == 0, "real decoded sample dates are never replaced by estimates");
        using var host = new Form { Size = new Size(1050, 800) };
        using var panel = new HistoryPanel(); host.Controls.Add(panel); host.Show();
        panel.SetContext(new PlayerDetail { Player = new PlayerRow { Uid = 1, Name = "Yacine Zidi" } }, true, (_, _) => Task.FromResult(history));
        Application.DoEvents();
        var table = All(panel).OfType<DataGridView>().Single();
        check(table.Rows.Count == 20 && table.Rows[0].Cells[0].Value!.ToString()!.Contains("2026") && table.Rows[0].Cells[0].Value!.ToString()!.Contains("ricostruito"), "internal history displays reconstructed months and a separate current point");
        check(table.Rows[19].Cells[0].Value!.ToString()!.Contains("10/02/2028") && history.EmbeddedPoints.All(p => p.Date == ""), "current values retain the actual save date");
        var toggle = All(panel).OfType<CheckBox>().Single(c => c.Name == "HistoryMonthlyCalendar");
        toggle.Checked = false; Application.DoEvents();
        check(table.Rows[0].Cells[0].Value!.ToString() == "Campione 1", "monthly display can switch back to sample numbers");
        toggle.Checked = true; Application.DoEvents();
        var selected = All(panel).OfType<CheckedListBox>().Single(); selected.SetItemChecked(0, true); selected.SetItemChecked(1, true); Application.DoEvents();
        check(table.Columns.Cast<DataGridViewColumn>().Any(c => c.HeaderText == "Dribbling") && table.Columns.Cast<DataGridViewColumn>().Any(c => c.HeaderText == "Tecnica"), "reconstructed timeline supports multiple attributes together");
        L10n.SetLanguage("en"); panel.SetLanguage(false); Application.DoEvents();
        check(toggle.Text == "Show reconstructed months" && table.Rows[0].Cells[0].Value!.ToString()!.Contains("reconstructed"), "reconstructed calendar labels switch to English");
        L10n.SetLanguage("it"); host.Close();
        using var about = new AboutForm(_ => Task.FromResult<ReleaseUpdate?>(null)); about.Show(); Application.DoEvents();
        check(about.ClientSize.Height < 380 * about.DeviceDpi / 96f && !All(about).OfType<Label>().Where(l => l is not LinkLabel).Any(l => l.Text.Contains("hierro1981") || l.Text.Contains("Progetto di")), "About removes author attribution and fits content in a shorter window");
        var privacy = All(about).OfType<Label>().Single(l => l.Text == L10n.Get("UpdatePrivacy"));
        check(privacy.PointToScreen(new Point(0, privacy.Height)).Y <= about.PointToScreen(new Point(0, about.ClientSize.Height)).Y, "compact About keeps privacy text visible");
        about.Close();
    }
}
