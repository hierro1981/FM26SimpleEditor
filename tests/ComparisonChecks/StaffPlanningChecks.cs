using FM26SimpleEditor;

internal static class StaffPlanningChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(All(child)));
    private static StaffRow Person(uint id, int quality = 10, string end = "2028-06-30", uint club = 1) => new()
    {
        Uid = id, Name = "Staff " + id, ClubUid = club, ClubName = "Club " + club, HasContract = true,
        JobCode = 48, JobKey = "sports_scientist", ContractStart = "2026-07-01", ContractEnd = end, Wage = 1000,
        ClubReputation = 4000, Attributes = StaffRecruitment.Roles.SelectMany(r => r.Weights.Keys)
            .Concat(StaffRecruitment.Training.SelectMany(r => r.Weights.Keys)).Distinct().ToDictionary(k => k, _ => quality)
    };
    public static void Run(Action<bool,string> check)
    {
        L10n.SetLanguage("it");
        var profile = StaffRecruitment.Roles.Single(p => p.Key == "sports_scientist");
        var market = new StaffRecruitmentResult { Status = "ready", ClubUid = 1, ClubReputation = 6308, GameDate = "2028-03-28" };
        foreach (var context in new[] { (Rep:6552, End:"2028-06-30", Loyalty:11), (Rep:6695, End:"2029-06-30", Loyalty:12) })
        {
            var person = Person(2, 19, context.End, 2); person.ClubReputation = context.Rep; person.Personality["loyalty"] = context.Loyalty;
            var estimate = StaffRecruitment.Estimate(person, profile, market);
            check(estimate.Band == StaffInterest.Unlikely && estimate.Score <= -21 && estimate.NoClearUpgrade &&
                estimate.Reasons.Any(r => r.Contains("contratto attivo")) && !estimate.Reasons.Any(r => r.Contains("Affidabilità limitata")),
                "reported recovery-staff refusal is not offset by near expiry " + context.Rep);
            check(StaffRecruitment.Estimate(person, profile, market, 100000).Band == StaffInterest.Unlikely,
                "salary cannot erase the prestige barrier " + context.Rep);
            market.Staff = [person];
            check(StaffRecruitment.Rank(market, profile).Count == 0 && StaffRecruitment.Rank(market, profile, ignoreInterest:true).Count == 1,
                "prestige-drop candidates remain accessible through ignore-interest " + context.Rep);
            person.ContractEnd = "2028-03-27";
            check(StaffRecruitment.Estimate(person, profile, market).Band == StaffInterest.Likely,
                "expired contracts do not inherit a current-employer barrier " + context.Rep);
        }
        var current = Person(1); var own = new StaffResult { Status = "ready", ClubUid = 1, GameDate = market.GameDate, Staff = [current] };
        var better = Person(2, 12, club:2); market.Staff = [better];
        var row = StaffPlanning.Build(own, market).Single();
        check(row.Advice == StaffRenewalAdvice.CompareReplacement && row.Quality == 10 && row.Alternatives.Single().Quality == 12 && row.BestPlausible?.Uid == 2,
            "renewal advice compares same-job quality with a stronger plausible recruit");
        var borderlineTop = Person(30,20,club:2); borderlineTop.ClubReputation=6308;
        market.Staff=[borderlineTop,better]; var proposed = StaffPlanning.Build(own,market).Single();
        check(proposed.Proposed?.Uid==better.Uid && proposed.Replacement==better.Name && proposed.ReplacementStars==proposed.Proposed.RoleStars &&
            proposed.ReplacementStars < proposed.Alternatives.First().RoleStars,
            "proposed stars belong to the named plausible alternative rather than a stronger borderline candidate");
        market.Staff=[better];
        better.ClubReputation = 6308;
        row = StaffPlanning.Build(own, market).Single();
        check(row.Advice == StaffRenewalAdvice.VerifyAlternatives && row.BestPlausible is null && row.Alternatives.Count == 1,
            "borderline alternatives alone never justify losing the incumbent");
        better.ClubReputation = null;
        check(StaffPlanning.Build(own, market).Single().Advice == StaffRenewalAdvice.VerifyAlternatives,
            "uncertain interest is shown for verification rather than a firm replacement");
        better.ClubReputation = 7000;
        check(StaffPlanning.Build(own, market).Single().Advice == StaffRenewalAdvice.Renew,
            "better but unlikely recruits do not undermine the renewal advice");
        better.ClubReputation = 4000; better.Attributes = new(current.Attributes); better.Attributes["sports_science"] = 11;
        check(StaffPlanning.Build(own, market).Single().BetterCount == 1, "meaningful quality advantage is measured on the shared /20 scale");
        better.Attributes["sports_science"] = 10; better.Attributes["analysing_data"] = 11;
        check(StaffPlanning.Build(own, market).Single().Advice == StaffRenewalAdvice.Renew, "tiny attribute differences are not treated as a meaningful upgrade");
        better = Person(2, 20, club:2); better.JobKey = "head_sports_science"; better.JobCode = 40; market.Staff = [better];
        check(StaffPlanning.Build(own, market).Single().BetterCount == 0, "a different current job is not silently assumed to accept a demotion");
        better.JobKey = "sports_scientist"; better.JobFamilyOnly = true;
        check(StaffPlanning.Build(own, market).Single().BetterCount == 0, "imprecise job categories do not establish a replacement");
        better.JobFamilyOnly = false;
        var affiliate = Person(3, 20, club:3); own.Staff = [current, affiliate]; market.Staff = [affiliate];
        check(StaffPlanning.Build(own, market).Single(p=>p.Uid==1).BetterCount == 0, "controlled-affiliate staff are excluded by UID even across employer rows");
        own.Staff = [current]; market.Staff = [better];
        current.Attributes.Remove("sports_science");
        check(StaffPlanning.Build(own, market).Single().Advice == StaffRenewalAdvice.Review && StaffPlanning.Build(own, market).Single().Quality is null,
            "missing incumbent inputs never cause an invented replacement recommendation");
        current.Attributes["sports_science"] = 10;
        check(StaffPlanning.Build(own, null).Single().Advice == StaffRenewalAdvice.Review, "market loading does not temporarily recommend renewal");
        market.GameDate = "2028-03-29";
        check(StaffPlanning.Build(own, market).Single().Advice == StaffRenewalAdvice.Review, "market from another save date is not used");
        market.GameDate = own.GameDate; market.ClubUid = 99;
        check(StaffPlanning.Build(own, market).Single().Advice == StaffRenewalAdvice.Review, "market from another managed club is not used");
        market.ClubUid = 1;
        var human = Person(10); human.IsHumanManager = true;
        var future = Person(11); future.ContractStart = "2028-04-01";
        var missing = Person(12); missing.HasContract = false;
        var invalid = Person(13); invalid.ContractEnd = "not a date";
        own.Staff = [current, Person(4,end:"2028-09-28"), Person(5,end:"2028-09-29"), Person(6,end:"2028-03-27"), Person(7,end:"2028-03-28"), human,future,missing,invalid];
        check(StaffPlanning.Build(own,market).Select(p=>p.Uid).OrderBy(p=>p).SequenceEqual(new uint[]{1,4,7}),
            "renewal window includes today and six calendar months, excludes expired, future, unreadable and human contracts");
        own.GameDate = "2027-08-31"; own.Staff = [Person(1,end:"2028-02-29"), Person(2,end:"2028-03-01")];
        check(StaffPlanning.Build(own,null).Single().Uid == 1, "calendar expiry handles month ends and leap years");
        own.GameDate = null;
        check(StaffPlanning.Build(own,market).Count == 0, "missing save date is not replaced by the computer date");
        var coach = Person(20); coach.JobKey = "coach"; coach.JobCode = 2; coach.Attributes["coaching_technical"] = 20;
        var chosen = StaffPlanning.DefaultProfile(coach)!;
        check(chosen.CategoryKey is not null && StaffRecruitment.DesiredJob(chosen) == "coach" &&
            StaffRecruitment.Quality(coach,chosen) == StaffPlanning.Profiles(coach).Where(p=>p.CategoryKey is not null).Max(p=>StaffRecruitment.Quality(coach,p)),
            "coach default uses the strongest relevant category, not a goalkeeper or fitness category");
        own.GameDate = market.GameDate; own.Staff = [coach];
        row = StaffPlanning.Build(own,market,choices:new Dictionary<uint,string>{{20,"coach"}}).Single();
        check(row.Profile?.Key == "coach" && row.Stars == new StaffCandidate(coach,row.Profile!,row.Quality,new(StaffInterest.Uncertain,0,[]),null).RoleStars,
            "criterion override uses the same job stars as recruitment");
        row = StaffPlanning.Build(own,market).Single();
        check(row.Stars == CoachingStars.Calculate(coach).Single(c=>c.Category.Key == row.Profile!.CategoryKey).Stars,
            "planning training stars equal the existing staff page");
        // Real integration path: staff first, then recruitment; no extra backend command.
        using var host = new Form {Size = new Size(1100,800)};
        using var panel = new MySquadPanel(); host.Controls.Add(panel);
        panel.SetData(new() { Status="ready", Clubs=[new(){ClubUid=1, ClubName="Club",FirstTeamId=10,TeamIds=[10]}] });
        int staffRequests=0, marketRequests=0;
        panel.StaffRequested += _ => staffRequests++; panel.RecruitmentRequested += _ => marketRequests++;
        host.Show(); var tabs = All(panel).OfType<TabControl>().Single(); tabs.SelectedIndex=5; Application.DoEvents();
        check(staffRequests==1 && marketRequests==0, "opening planning loads own staff before requesting market");
        own.Staff = [current]; panel.SetStaffData(own); panel.EnsureRecruitmentLoaded();
        check(marketRequests==1, "planning chains existing recruitment after own staff completes");
        panel.SetRecruitmentData(market); Application.DoEvents();
        var grid = All(panel).OfType<DataGridView>().Single(g=>g.Name=="PlanList");
        var alts = All(panel).OfType<DataGridView>().Single(g=>g.Name=="PlanAlternatives");
        var attrs = All(panel).OfType<DataGridView>().Single(g=>g.Name=="PlanAttributes");
        check(grid.Rows.Count==1 && alts.Rows.Count==1 && attrs.Rows.Count==2 && grid.Columns["Stars"].CellTemplate is StarRatingCell &&
            grid.ReadOnly && grid.ScrollBars==ScrollBars.Both && alts.ScrollBars==ScrollBars.Both,
            "planning shows renewal advice, stronger alternatives and attribute comparison with scrolling and stars");
        check(!grid.Columns.Contains("Club") && grid.Columns["ReplacementStars"].CellTemplate is StarRatingCell &&
            grid.Rows[0].Cells["ReplacementStars"].Value is double replacementStars && replacementStars==((StaffRenewal)grid.Rows[0].DataBoundItem).Proposed?.RoleStars,
            "renewal grid removes own-club column and shows the proposed candidate stars");
        check(!All(panel).OfType<ComboBox>().Single(c=>c.Name=="MySquadScope").Enabled, "player squad scope is disabled for staff planning");
        foreach(int width in new[]{1100,760})
        {
            host.Width=width; Application.DoEvents();
            check(grid.Visible && alts.Visible && attrs.Visible && grid.Height>60 && alts.Height>60 && attrs.Width>180,
                "planning keeps current contracts and alternatives visible at width "+width);
        }
        L10n.SetLanguage("en"); panel.SetLanguage(); Application.DoEvents();
        check(tabs.TabPages[5].Text=="Staff planning" && grid.Columns["Recommendation"].HeaderText=="Renewal advice" && grid.Rows.Count==1,
            "planning language change translates captions and keeps the data");
        panel.SetRecruitmentData(new(){Status="ready",ClubUid=99});
        check(alts.Rows.Count==1, "planning ignores stale market responses");
        own.Staff = [coach]; panel.SetStaffData(own);
        var criteria = All(panel).OfType<ComboBox>().Single(c=>c.Name=="PlanCriterion");
        check(((StaffSearchProfile)criteria.SelectedItem!).CategoryKey is not null, "planning UI defaults a coach to a relevant training category");
        criteria.SelectedIndex = criteria.Items.Cast<StaffSearchProfile>().ToList().FindIndex(p=>p.Key=="coach");
        L10n.SetLanguage("it"); panel.SetLanguage(); Application.DoEvents();
        check(((StaffSearchProfile)criteria.SelectedItem!).Key=="coach" && ((StaffRenewal)grid.Rows[0].DataBoundItem).Profile?.Key=="coach",
            "changing renewal criterion updates advice and survives language changes");
        var button=All(panel).OfType<Button>().Single(b=>b.Name=="PlanFindStaff");
        var recruitProfile=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitProfile");
        var recruitMode=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitMode");
        var recruitJob=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitCurrentJob");
        var ignore=All(panel).OfType<CheckBox>().Single(c=>c.Name=="RecruitIgnoreInterest");
        var recruitSearch=All(panel).OfType<TextBox>().Single(c=>c.Name=="RecruitSearch");
        var planOffer=All(panel).OfType<NumericUpDown>().Single(c=>c.Name=="PlanOffer");
        var recruitOffer=All(panel).OfType<NumericUpDown>().Single(c=>c.Name=="RecruitWage");
        ignore.Checked=true; recruitSearch.Text="previous unrelated search"; planOffer.Value=4321;
        criteria.SelectedIndex=criteria.Items.Cast<StaffSearchProfile>().ToList().FindIndex(p=>p.CategoryKey is not null);
        var selectedTraining=((StaffSearchProfile)criteria.SelectedItem!).Key;
        button.PerformClick(); Application.DoEvents();
        check(tabs.SelectedIndex==4 && recruitMode.SelectedIndex==1 && ((StaffSearchProfile)recruitProfile.SelectedItem!).Key==selectedTraining &&
            recruitJob.SelectedItem!.ToString()==L10n.Get("StaffJob_coach") && recruitOffer.Value==4321 && !ignore.Checked && recruitSearch.Text.Length==0,
            "planning opens recruitment with its training criterion, current job, salary and active interest filtering, clearing stale text");
        tabs.SelectedIndex=5; criteria.SelectedIndex=criteria.Items.Cast<StaffSearchProfile>().ToList().FindIndex(p=>p.Key=="coach");
        button.PerformClick(); Application.DoEvents();
        check(tabs.SelectedIndex==4 && recruitMode.SelectedIndex==0 && ((StaffSearchProfile)recruitProfile.SelectedItem!).Key=="coach",
            "planning job criterion switches recruitment back from training to job search");
        tabs.SelectedIndex=5;
        panel.ClearForSave(false); Application.DoEvents();
        check(!button.Enabled, "planning search shortcut is disabled without a selected renewal");
        check(grid.Rows.Count==0 && alts.Rows.Count==0 && attrs.Rows.Count==0, "opening another save clears staff planning and alternatives");
        host.Close(); L10n.SetLanguage("it");
    }
}
