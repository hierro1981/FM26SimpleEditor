using FM26SimpleEditor;

internal static class StaffRecruitmentChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static StaffRow Person(uint uid, int rep = 5000, int value = 15) => new()
    {
        Uid = uid, Name = "Person " + uid, ClubUid = 2, ClubName = "Other", ClubReputation = rep, HasContract = true,
        ContractStart = "2025-07-01", ContractEnd = "2029-06-30", Ca = 120, Wage = 1000, JobCode = 14, JobKey = "scout",
        Attributes = StaffRecruitment.Roles.SelectMany(r => r.Weights.Keys).Concat(StaffRecruitment.Training.SelectMany(r => r.Weights.Keys)).Distinct().ToDictionary(k => k, _ => value)
    };
    public static void Run(Action<bool,string> check)
    {
        L10n.SetLanguage("it");
        var scout = StaffRecruitment.Roles.Single(r => r.Key == "scout");
        var data = new StaffRecruitmentResult { Status = "ready", ClubUid = 1, ClubName = "Destination", ClubReputation = 5000, GameDate = "2028-03-04" };
        var good = Person(1); var poor = Person(2,5000,5); var tooHigh = Person(3,10000,20); var own = Person(4); own.ClubUid = 1;
        var human = Person(5); human.IsHumanManager = true;
        data.Staff = [poor, good, tooHigh, own, human];
        var ranking = StaffRecruitment.Rank(data,scout);
        check(ranking.Select(c => c.Uid).SequenceEqual(new uint[] {1,2}) && ranking.All(c => c.Interest.Band == StaffInterest.Borderline), "recruitment ranks quality among plausible and borderline candidates without own staff or human managers");
        check(StaffRecruitment.Rank(data,scout,ignoreInterest:true).First().Uid == 3, "unlikely toggle exposes top-quality hard targets without changing quality scores");
        check(StaffRecruitment.Estimate(tooHigh,scout,data).Band == StaffInterest.Unlikely && StaffRecruitment.Estimate(tooHigh,scout,data,2000).Band == StaffInterest.Borderline,
            "explicit salary scenario can move a marginal rejection into borderline without claiming acceptance");
        var unknown=Person(6); unknown.ClubReputation=null; unknown.HasContract=false;
        check(StaffRecruitment.Estimate(unknown,scout,data).Band == StaffInterest.Uncertain && StaffRecruitment.Estimate(unknown,scout,data).Reasons.Any(r => r.Contains("non equivale")), "missing contract never means unemployed and missing reputation is uncertainty");
        data.Staff=[good,unknown];
        check(StaffRecruitment.Rank(data,scout).Count==2, "uncertain targets remain visible by default for validation");
        unknown.Attributes.Remove("judging_potential"); unknown.Ca=200;
        check(StaffRecruitment.Quality(unknown,scout) is null && StaffRecruitment.Rank(data,scout).Last().Uid==6, "missing primary attributes are not substituted by CA or ranked ahead of complete candidates");
        var duplicate=Person(1,10000); data.Staff=[good,duplicate];
        check(StaffRecruitment.Rank(data,scout).Count==0 && StaffRecruitment.Rank(data,scout,ignoreInterest:true).Count==1, "multiple employments use conservative prestige and produce only one shortlist row per person");
        var coach = Person(7); coach.JobKey = "coach"; coach.JobCode = 2;
        var unidentified = Person(8); unidentified.JobKey = null; unidentified.JobCode = null;
        data.Staff = [good, coach, unidentified, tooHigh];
        check(StaffRecruitment.Rank(data, scout, currentJob: "scout").Select(c => c.Uid).SequenceEqual(new uint[] {1}),
            "current-job filter uses decoded employment independently from the desired profile and interest");
        check(StaffRecruitment.Rank(data, scout, ignoreInterest:true, currentJob: "coach").Single().Uid == 7 &&
            StaffRecruitment.Rank(data, scout, currentJob:"__unknown").Single().Uid == 8,
            "ignore-interest preserves current-job filtering and unidentified jobs remain explicitly selectable");
        check(StaffRecruitment.Rank(data, scout, currentJob: "").Count == StaffRecruitment.Rank(data, scout).Count,
            "any-current-job default retains the full eligible candidate pool");
        var feedbackDestination = new StaffRecruitmentResult { ClubReputation = 6302, GameDate = "2028-03-08" };
        foreach (var sample in new[] { (Rep:7957, Ambition:16, Loyalty:17), (Rep:8582, Ambition:11, Loyalty:8) })
        {
            var target = Person(9, sample.Rep); target.Personality = new() { ["ambition"] = sample.Ambition, ["loyalty"] = sample.Loyalty };
            var estimate = StaffRecruitment.Estimate(target, scout, feedbackDestination);
            check(estimate.Score == -17 && estimate.Band == StaffInterest.Borderline && estimate.Label.Contains("rischio rifiuto elevato") && estimate.Reasons.Any(r => r.Contains("meno prestigioso")),
                "reported refusal context is cautioned without per-person exceptions or changing the uncalibrated score " + sample.Rep);
        }
        feedbackDestination.ClubNationId = 150;
        foreach (var sample in new[] { (Rep:6259, Loyalty:10, Country:139, Score:-2), (Rep:6317, Loyalty:12, Country:150, Score:-2), (Rep:5950, Loyalty:5, Country:120, Score:1) })
        {
            var target = Person(10, sample.Rep); target.ClubNationId = sample.Country;
            target.Personality = new() { ["loyalty"] = sample.Loyalty }; target.Attributes["adaptability"] = 16;
            var estimate = StaffRecruitment.Estimate(target, scout, feedbackDestination);
            check(estimate.Score == sample.Score && estimate.Band == StaffInterest.Borderline && estimate.NoClearUpgrade && estimate.Label.Contains("miglioramento non evidente"),
                "near-equal club reputation does not imply a career improvement " + sample.Rep);
            check(estimate.Reasons.Any(r => r.Contains("paesi diversi")) == (sample.Country != 150),
                "relocation uncertainty uses current employer country even with high adaptability " + sample.Country);
        }
        var lateral = Person(11,6259);
        check(StaffRecruitment.Estimate(lateral,scout,feedbackDestination,2000).Band == StaffInterest.Borderline,
            "a higher wage cannot alone promote a lateral move to plausibly interested");
        var resident = Person(12); resident.NationId = 120; resident.ClubNationId = 150; resident.Attributes["adaptability"] = 1;
        var residentEstimate = StaffRecruitment.Estimate(resident,scout,feedbackDestination);
        resident.NationId = 150;
        check(residentEstimate.Score == StaffRecruitment.Estimate(resident,scout,feedbackDestination).Score && !residentEstimate.Reasons.Any(r=>r.Contains("paesi diversi")),
            "foreign nationality alone does not invent a relocation penalty when current club country is unchanged");
        var enriched = Person(13);
        enriched.Reputation = new() { ["home"] = 7445, ["current"] = 7445, ["world"] = 3993 };
        enriched.JobPreferences = new() { ["manager"] = 11, ["assistant_manager"] = 13, ["coach"] = 20, ["physio"] = 1, ["scout"] = 1 };
        var technical = StaffRecruitment.Training.Single(p => p.CategoryKey == "PossessionTechnical");
        check(enriched.CurrentReputation == 7445 && StaffRecruitment.JobPreference(enriched, technical) == 20 && StaffRecruitment.JobPreference(enriched, scout) == 1,
            "personal reputation and saved job affinity stay separate from current club prestige and coaching suitability");
        int baseline = StaffRecruitment.Estimate(enriched,technical,data).Score;
        check(StaffRecruitment.Estimate(enriched,technical,data).Reasons.Any(r=>r.Contains("7445")) && StaffRecruitment.Estimate(enriched,technical,data).Reasons.Any(r=>r.Contains("20/20")),
            "assessment exposes decoded personal reputation and the affinity for the implied coach job");
        enriched.Reputation.Clear(); enriched.JobPreferences.Clear();
        check(StaffRecruitment.Estimate(enriched,technical,data).Score == baseline && enriched.CurrentReputation is null && StaffRecruitment.JobPreference(enriched, technical) is null,
            "newly read profile facts do not invent an acceptance formula or missing affinity values");
        data.Staff=[good,poor];
        check(StaffRecruitment.Rank(data,scout,search:"Person 2").Single().Uid==2, "recruitment search uses actual candidate identity");
        foreach(var category in StaffRecruitment.Training)
        {
            var row=StaffRecruitment.Rank(data,category).First();
            check(row.RoleStars==CoachingStars.Calculate(good).Single(c=>c.Category.Key==category.CategoryKey).Stars, "recruitment and staff share training stars " + category.Key);
        }
        check(StaffRecruitment.Roles.Select(r=>r.Key).Distinct().Count()==StaffRecruitment.Roles.Count && StaffRecruitment.Roles.All(r=>L10n.Keys.Contains("StaffJob_"+r.Key)), "every job profile has a unique translated supported label");
        var doctor=StaffRecruitment.Roles.Single(r=>r.Key=="doctor"); var med=Person(10); med.JobKey="doctor"; data.Staff=[good,med];
        check(StaffRecruitment.Rank(data,doctor).Single().RoleStars is null, "doctor category never borrows physiotherapy to invent suitability stars");
        good.ContractEnd="2028-01-01";
        check(StaffRecruitment.Estimate(good,scout,data).Reasons.Any(r=>r.Contains("scaduto")), "expired contract is explained without applying current-employer prestige");
        data.Staff=[good,poor];
        using var host=new Form {Size=new Size(950,750)}; using var panel=new StaffRecruitmentPanel(); host.Controls.Add(panel);
        panel.ClearForClub(1); panel.SetData(data); host.Show(); Application.DoEvents();
        var grid=All(panel).OfType<DataGridView>().Single(g=>g.Name=="RecruitList");
        check(grid.Rows.Count==2 && grid.ReadOnly && grid.ScrollBars==ScrollBars.Both && grid.Columns["RoleStars"].CellTemplate is StarRatingCell, "recruitment UI shows sortable scrollable candidate and suitability-star columns");
        var mode=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitMode");mode.SelectedIndex=1;Application.DoEvents();
        var choice=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitProfile");
        check(choice.Items.Count==9 && grid.Rows.Count==2, "training mode offers every shared training category");
        foreach(int width in new[]{950,760})
        {
            host.Width=width;Application.DoEvents();
            var lower=All(panel).OfType<TableLayoutPanel>().Single(c=>c.Name=="RecruitDetailColumns");
            var attrs=All(panel).OfType<DataGridView>().Single(g=>g.Name=="RecruitAttributes");
            var stars=All(panel).OfType<DataGridView>().Single(g=>g.Name=="RecruitCoaching");
            check(grid.Height>60 && attrs.Height>20 && stars.Height>60 && lower.ClientSize.Width>500 && attrs.Visible && stars.Visible, "recruitment retains candidate and detail space at width "+width);
        }
        grid.Rows[0].Selected=true;Application.DoEvents();
        check(All(panel).OfType<DataGridView>().Single(g=>g.Name=="RecruitCoaching").Rows.Count==9 && All(panel).OfType<Label>().Single(l=>l.Name=="RecruitReasons").Text.Contains("Person"), "selection displays candidate reasons and all coaching categories together");
        L10n.SetLanguage("en");panel.SetLanguage();Application.DoEvents();
        check(mode.SelectedIndex==1 && choice.Items.Count==9 && grid.Columns["InterestLabel"].HeaderText=="Estimated interest", "language changes preserve training selection and translate recruitment headers");
        data.Staff = [good, poor, tooHigh]; panel.SetData(data); Application.DoEvents();
        var ignore = All(panel).OfType<CheckBox>().Single(c => c.Name == "RecruitIgnoreInterest");
        check(!ignore.Checked && grid.Rows.Count == 2, "interest filter remains active by default");
        ignore.Checked = true; Application.DoEvents();
        check(grid.Rows.Count == 3 && ((StaffCandidate)grid.Rows[0].DataBoundItem).Uid == 3,
            "ignore-interest checkbox restores quality order and exposes the strongest candidate even when unlikely");
        L10n.SetLanguage("it"); panel.SetLanguage(); Application.DoEvents();
        check(ignore.Checked && ignore.Text.Contains("Ignora interesse") && grid.Rows.Count == 3, "ignore-interest choice survives language changes");
        ignore.Checked = false; Application.DoEvents();
        check(grid.Rows.Count == 2, "unchecking ignore-interest restores the plausible and borderline pool");
        data.Staff = [good, coach, unidentified]; panel.SetData(data); Application.DoEvents();
        var jobs = All(panel).OfType<ComboBox>().Single(c => c.Name == "RecruitCurrentJob");
        check(jobs.SelectedIndex == 0 && jobs.Items[0].ToString() == "Qualsiasi", "current-job selector defaults to any");
        jobs.SelectedIndex = jobs.Items.Cast<object>().ToList().FindIndex(c => c.ToString() == L10n.Get("StaffJob_coach")); Application.DoEvents();
        check(grid.Rows.Count == 1 && ((StaffCandidate)grid.Rows[0].DataBoundItem).Uid == 7, "current-job dropdown filters the displayed shortlist");
        L10n.SetLanguage("en"); panel.SetLanguage(); Application.DoEvents();
        check(jobs.SelectedItem!.ToString() == "Coach" && grid.Rows.Count == 1 && jobs.Items[0].ToString() == "Any", "language changes preserve the current-job filter key");
        jobs.SelectedIndex = 0; Application.DoEvents();
        check(grid.Rows.Count == 3, "any-current-job option restores candidates from other and unidentified jobs");
        good.Reputation = new() { ["home"] = 6461, ["current"] = 6461, ["world"] = 3365 };
        good.JobPreferences = new() { ["coach"] = 15 };
        data.Staff = [good]; panel.SetData(data); Application.DoEvents();
        check(grid.Rows[0].Cells["CurrentReputation"].Value?.ToString() == "6461" && grid.Rows[0].Cells["JobPreference"].Value?.ToString() == "15",
            "shortlist displays personal reputation and the category's saved coach affinity");
        check(All(panel).OfType<DataGridView>().Single(g=>g.Name=="RecruitAttributes").Rows.Cast<DataGridViewRow>().Any(r=>r.Cells[1].Value?.ToString()=="3365"),
            "recruitment details expose all decoded reputation figures rather than only club reputation");
        panel.ClearForClub(9);panel.SetData(data);
        check(grid.Rows.Count==0, "stale recruitment responses from another club are ignored");
        host.Close();L10n.SetLanguage("it");
    }
}
