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
        current.TeamId=100; current.TeamSlot=0; better.TeamId=101;better.TeamSlot=1;better.TeamName="U20";
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
        better = Person(2, 20, club:2); better.TeamId=101;better.TeamSlot=1;better.TeamName="U20"; better.JobKey = "head_sports_science"; better.JobCode = 40; market.Staff = [better];
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
        check(StaffPlanning.Build(own,market).Select(p=>p.Uid).OrderBy(p=>p).SequenceEqual(new uint[]{1,7}),
            "renewal window includes today and excludes six-month boundary, expired, future, unreadable and human contracts");
        own.GameDate = "2027-08-31"; own.Staff = [Person(1,end:"2028-02-28"), Person(2,end:"2028-02-29")];
        check(StaffPlanning.Build(own,null).Single().Uid == 1, "calendar expiry handles month ends and leap years");
        var clock = new DateTime(2028,3,28);
        check(StaffPlanning.InWindow(clock.AddDays(-20),clock,StaffContractWindow.Expired) &&
            StaffPlanning.InWindow(clock.AddDays(2),clock,StaffContractWindow.Expired) &&
            !StaffPlanning.InWindow(clock.AddDays(3),clock,StaffContractWindow.Expired),
            "expired window includes overdue and fewer than three days left, excluding exact boundary");
        check(!StaffPlanning.InWindow(clock.AddDays(-1),clock,StaffContractWindow.SixMonths) &&
            StaffPlanning.InWindow(clock,clock,StaffContractWindow.SixMonths) &&
            StaffPlanning.InWindow(clock.AddMonths(6).AddDays(-1),clock,StaffContractWindow.SixMonths) &&
            !StaffPlanning.InWindow(clock.AddMonths(6),clock,StaffContractWindow.SixMonths) &&
            StaffPlanning.InWindow(clock.AddMonths(12).AddDays(-1),clock,StaffContractWindow.TwelveMonths) &&
            !StaffPlanning.InWindow(clock.AddMonths(12),clock,StaffContractWindow.TwelveMonths),
            "six and twelve month windows use strict calendar boundaries and exclude expired contracts");
        own.GameDate="2028-03-28";
        own.Staff=[Person(1,end:"2028-03-01"),Person(2,end:"2028-03-30"),Person(3,end:"2028-03-31"),
            Person(4,end:"2028-12-01"),Person(5,end:"2030-01-01"),human,future,missing,invalid];
        check(StaffPlanning.Build(own,null,window:StaffContractWindow.Expired).Select(p=>p.Uid).SequenceEqual(new uint[]{1,2}) &&
            StaffPlanning.Build(own,null,window:StaffContractWindow.TwelveMonths).Count==3 &&
            StaffPlanning.Build(own,null,window:StaffContractWindow.All).Count==5,
            "all windows include only readable employee contracts and respect future-start and human exclusions");
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
        check(grid.Rows[0].Cells["Team"].Value?.ToString()=="Prima squadra" && grid.Rows[0].Cells["ReplacementTeam"].Value?.ToString()=="U20" &&
            alts.Rows[0].Cells["Team"].Value?.ToString()=="U20", "planning exposes incumbent and proposed candidate squads in every list");
        check(grid.Columns["Criterion"].DisplayIndex==grid.Columns.Count-1 &&
            new[]{"Role","Team","ContractEnd","Days","Criterion","Quality","Replacement","BetterCount"}.All(k=>grid.Columns[k].AutoSizeMode==DataGridViewAutoSizeColumnMode.AllCellsExceptHeader) &&
            grid.Columns["Quality"].Width<95 && grid.Columns["Days"].Width<85 && grid.Columns["Team"].Width<210,
            "planning compacts content-sized columns and places comparison criterion last");
        check(!grid.Columns.Contains("Club") && grid.Columns["ReplacementStars"].CellTemplate is StarRatingCell &&
            grid.Rows[0].Cells["ReplacementStars"].Value is double replacementStars && replacementStars==((StaffRenewal)grid.Rows[0].DataBoundItem).Proposed?.RoleStars,
            "renewal grid removes own-club column and shows the proposed candidate stars");
        check(!All(panel).OfType<ComboBox>().Single(c=>c.Name=="MySquadScope").Enabled, "player squad scope is disabled for staff planning");
        var expiry = All(panel).OfType<ComboBox>().Single(c=>c.Name=="PlanExpiry");
        check(expiry.SelectedIndex==(int)StaffContractWindow.SixMonths && expiry.Items.Count==4,
            "planning starts with the six-month expiry filter");
        var highest=Person(31,20,club:2);highest.ClubReputation=6308;
        var middle=Person(32,15,club:2);middle.ClubReputation=4000;
        var lower=Person(33,12,club:2);lower.ClubReputation=4000;
        market.Staff=[lower,middle,highest];panel.SetRecruitmentData(market);
        check(alts.Rows.Cast<DataGridViewRow>().Select(r=>((StaffCandidate)r.DataBoundItem).Quality).SequenceEqual(new double?[]{20,15,12}),
            "alternatives default to score descending even when the strongest has borderline interest");
        var headerClick=typeof(DataGridView).GetMethod("OnColumnHeaderMouseClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        void SortAlternative(string column) => headerClick.Invoke(alts,[new DataGridViewCellMouseEventArgs(alts.Columns[column].Index,-1,0,0,new MouseEventArgs(MouseButtons.Left,1,0,0,0))]);
        SortAlternative("Quality");
        check(((StaffCandidate)alts.Rows[0].DataBoundItem).Quality==12 && alts.Columns["Quality"].HeaderCell.SortGlyphDirection==SortOrder.Ascending,
            "alternative score header toggles to numeric ascending order");
        foreach(string column in new[]{"Name","Club","CurrentRole","Team","RoleStars","InterestLabel","ClubReputation","Wage","MainAttributes"})
        {
            SortAlternative(column);
            check(alts.Columns[column].SortMode==DataGridViewColumnSortMode.Programmatic && alts.Columns[column].HeaderCell.SortGlyphDirection!=SortOrder.None,
                "alternative column is sortable: "+column);
        }
        SortAlternative("Quality");
        check(alts.Rows.Count==3 && alts.Columns["Quality"].HeaderCell.SortGlyphDirection==SortOrder.Descending,
            "returning to score restores descending order");
        check(!grid.ShowCellToolTips && !alts.ShowCellToolTips && grid.Columns.Contains("Info") && alts.Columns.Contains("Info") &&
            StaffPlanningPanel.InfoText(alts.Rows[0].DataBoundItem).Split('\n').All(line=>line.Length<=85),
            "only dedicated information cells show compact wrapped details, disabling native clipped-cell tooltips");
        own.Staff=[current,Person(41,end:"2028-03-20"),Person(42,end:"2028-12-01"),Person(43,end:"2030-01-01")];panel.SetStaffData(own);
        check(grid.Rows.Count==1,"default expiry filter excludes past and long contracts");
        expiry.SelectedIndex=(int)StaffContractWindow.Expired;
        check(grid.Rows.Count==1 && ((StaffRenewal)grid.Rows[0].DataBoundItem).Uid==41,"expiry combo shows expired staff");
        expiry.SelectedIndex=(int)StaffContractWindow.TwelveMonths;
        check(grid.Rows.Count==2,"expiry combo expands to twelve months");
        expiry.SelectedIndex=(int)StaffContractWindow.All;
        check(grid.Rows.Count==4,"expiry combo shows all employee contracts");
        L10n.SetLanguage("en");panel.SetLanguage();
        check(expiry.SelectedIndex==(int)StaffContractWindow.All && expiry.SelectedItem?.ToString()=="All" && grid.Rows.Count==4,
            "language switch preserves the expiry window and translates choices");
        L10n.SetLanguage("it");panel.SetLanguage();expiry.SelectedIndex=(int)StaffContractWindow.SixMonths;
        own.Staff=[current];market.Staff=[better];panel.SetStaffData(own);panel.SetRecruitmentData(market);
        current.TeamSlot=1;current.TeamName="U20";current.Attributes["working_with_youngsters"]=10;
        better.Attributes["working_with_youngsters"]=16;panel.SetStaffData(own);panel.SetRecruitmentData(market);Application.DoEvents();
        check(attrs.Rows[0].Cells[0].Value?.ToString()==L10n.Get("StaffAttribute_working_with_youngsters") &&
            attrs.Rows[0].Cells["Current"].Value?.ToString()=="10" && attrs.Rows[0].Cells["Alternative"].Value?.ToString()=="16" &&
            attrs.Rows[0].Cells["Delta"].Value?.ToString()=="+6" && attrs.Rows[0].Cells["Delta"].Style.ForeColor==Color.DarkGreen,
            "subteam comparison highlights working with youngsters for both people and its coloured delta");
        better.Attributes.Remove("working_with_youngsters");panel.SetRecruitmentData(market);
        check(attrs.Rows[0].Cells["Alternative"].FormattedValue?.ToString()=="—" && attrs.Rows[0].Cells["Delta"].Value?.ToString()=="—",
            "missing alternative youth-work ability is explicit rather than substituted");
        better.Attributes["working_with_youngsters"]=16;current.TeamSlot=0;current.TeamName=null;panel.SetStaffData(own);panel.SetRecruitmentData(market);
        check(!attrs.Rows.Cast<DataGridViewRow>().Any(r=>r.Cells[0].Value?.ToString()==L10n.Get("StaffAttribute_working_with_youngsters")),
            "first-team comparison keeps its original profile without an unrelated extra attribute");
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
        var doubleClick=typeof(DataGridView).GetMethod("OnCellDoubleClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        doubleClick.Invoke(grid,[new DataGridViewCellEventArgs(0,-1)]);
        doubleClick.Invoke(grid,[new DataGridViewCellEventArgs(grid.Columns["CopyName"].Index,0)]);
        check(tabs.SelectedIndex==5,"planning header and copy-name double clicks do not open recruitment");
        doubleClick.Invoke(grid,[new DataGridViewCellEventArgs(grid.Columns["Name"].Index,0)]); Application.DoEvents();
        check(tabs.SelectedIndex==4 && recruitMode.SelectedIndex==0 && ((StaffSearchProfile)recruitProfile.SelectedItem!).Key=="coach",
            "planning row double click uses the same filtered job search as the dedicated button");
        tabs.SelectedIndex=5;
        panel.ClearForSave(false); Application.DoEvents();
        check(!button.Enabled, "planning search shortcut is disabled without a selected renewal");
        check(grid.Rows.Count==0 && alts.Rows.Count==0 && attrs.Rows.Count==0, "opening another save clears staff planning and alternatives");
        host.Close(); L10n.SetLanguage("it");
    }
}
