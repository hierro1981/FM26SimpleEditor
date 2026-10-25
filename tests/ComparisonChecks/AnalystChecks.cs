using FM26SimpleEditor;

internal static class AnalystChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child=>new[]{child}.Concat(All(child)));
    private static StaffRow Analyst(uint uid,int code,int value,uint club=2) => new()
    {
        Uid=uid,Name="Analyst "+uid,ClubUid=club,ClubReputation=3000,HasContract=true,JobCode=code,JobKey="analyst",JobFamilyOnly=true,
        IsPlayerJob=(code&1)==1,ContractStart="2027-01-01",ContractEnd="2028-06-30",TeamId=uid+100,TeamSlot=1,TeamName="U20",
        Attributes=new(){["analysing_data"]=value,["tactical_knowledge"]=value,["judging_ability"]=value,["working_with_youngsters"]=12}
    };
    public static void Run(Action<bool,string> check)
    {
        L10n.SetLanguage("it");
        var a=Analyst(1,58,10,1);var b=Analyst(2,60,10,1);var c=Analyst(3,62,10,1);
        var x=Analyst(11,58,14);var y=Analyst(12,60,15);var dual=Analyst(13,61,17);var z=Analyst(14,62,16);
        var market=new StaffRecruitmentResult{Status="ready",ClubUid=1,ClubReputation=6000,GameDate="2028-03-04",Staff=[x,y,dual,z]};
        var own=new StaffResult{Status="ready",ClubUid=1,GameDate=market.GameDate,Staff=[a,b,c]};
        var rows=StaffPlanning.Build(own,market);
        check(rows.Count==3 && rows.All(r=>r.Profile?.Key=="analyst" && r.Quality==10 && r.Stars==2.5 && r.Advice==StaffRenewalAdvice.CompareReplacement),
            "known category-only analysts receive generic suitability, stars and renewal suggestions");
        check(rows.Single(r=>r.Uid==1).Alternatives.Select(r=>r.Uid).SequenceEqual(new uint[]{11}) &&
            rows.Single(r=>r.Uid==2).Alternatives.Select(r=>r.Uid).OrderBy(i=>i).SequenceEqual(new uint[]{12,13}) &&
            rows.Single(r=>r.Uid==3).Alternatives.Single().Uid==14,
            "analyst renewal caches keep native variants separate and preserve the dual-role bit semantics");
        check(rows.All(r=>r.Criterion.Contains("categoria") && r.Explanation.Contains("generica")) && b.RoleTooltip.Contains("60") && b.CurrentRoleStars==2.5,
            "analyst ratings explicitly remain category estimates with readable variant evidence");
        b.Attributes.Remove("judging_ability");
        var missing=StaffPlanning.Build(own,market).Single(r=>r.Uid==2);
        check(missing.Advice==StaffRenewalAdvice.Review && missing.Alternatives.Count==0 && missing.Explanation.Contains(L10n.Get("StaffAttribute_judging_ability")) && b.CurrentRoleStars is null,
            "missing analyst inputs retain review status with the exact missing attribute");
        b.Attributes["judging_ability"]=10;
        var unknown=Analyst(4,64,10,1);own.Staff=[unknown];
        check(StaffPlanning.Build(own,market).Single().Advice==StaffRenewalAdvice.Review && unknown.CurrentRoleStars is null,
            "unrecognised analyst variants do not gain an invented current-job profile");
        var doctor=Analyst(5,38,10,1);doctor.JobKey="doctor";own.Staff=[doctor];
        check(StaffPlanning.Build(own,market).Single().Advice==StaffRenewalAdvice.Review && doctor.CurrentRoleStars is null,
            "allowing known analyst categories does not remove doctor quality safeguards");
        own.Staff=[a,b,c];
        var profile=StaffRecruitment.Roles.Single(r=>r.Key=="analyst");
        check(StaffRecruitment.Rank(market,profile,currentJob:"analyst").Count==4 &&
            StaffRecruitment.Rank(market,profile,currentJob:"analyst",currentJobCode:60).Count==2 &&
            StaffRecruitment.Rank(market,profile,ignoreInterest:true,currentJob:"analyst",currentJobCode:61).Count==2,
            "generic analyst searches include all variants while specific and ignore-interest searches retain their exact variant");
        using var host=new Form{Size=new Size(1100,850)};using var squad=new MySquadPanel();host.Controls.Add(squad);
        squad.SetData(new(){Status="ready",Clubs=[new(){ClubUid=1,ClubName="Club",FirstTeamId=10,TeamIds=[10]}]});
        var tabs=All(squad).OfType<TabControl>().Single();host.Show();tabs.SelectedIndex=5;
        squad.SetStaffData(own);squad.SetRecruitmentData(market);Application.DoEvents();
        var plan=All(squad).OfType<DataGridView>().Single(g=>g.Name=="PlanList");
        var row=plan.Rows.Cast<DataGridViewRow>().Single(r=>((StaffRenewal)r.DataBoundItem).Uid==2);plan.ClearSelection();row.Selected=true;Application.DoEvents();
        check(plan.Rows.Count==3 && All(squad).OfType<Label>().Single(l=>l.Name=="PlanExplanation").Text.Contains("generica"),
            "planning UI shows analyst comparison and its category limitation");
        All(squad).OfType<Button>().Single(btn=>btn.Name=="PlanFindStaff").PerformClick();Application.DoEvents();
        var jobs=All(squad).OfType<ComboBox>().Single(cb=>cb.Name=="RecruitCurrentJob");
        var list=All(squad).OfType<DataGridView>().Single(g=>g.Name=="RecruitList");
        check(tabs.SelectedIndex==4 && jobs.SelectedItem!.ToString()==L10n.Format("RecruitAnalystVariantChoice",60) && list.Rows.Count==2 &&
            list.Rows.Cast<DataGridViewRow>().All(r=>StaffRecruitment.AnalystJob(((StaffCandidate)r.DataBoundItem).Person)==60),
            "planning-to-search shortcut carries the incumbent analyst's variant");
        L10n.SetLanguage("en");squad.SetLanguage();Application.DoEvents();
        check(jobs.SelectedItem!.ToString()=="Analyst · variant 60" && list.Rows.Count==2,"analyst variant selection survives a language change");
        jobs.SelectedIndex=jobs.Items.Cast<object>().ToList().FindIndex(o=>o.ToString()==L10n.Get("StaffJob_analyst"));Application.DoEvents();
        check(list.Rows.Count==4,"selecting generic Analyst restores all readable variants");
        host.Close();L10n.SetLanguage("it");
    }
}
