using FM26SimpleEditor;

internal static class StaffSquadSearchChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child=>new[]{child}.Concat(All(child)));
    private static StaffRow Person(uint uid, string? squad, int? slot, int youth=15) => new()
    {
        Uid=uid,Name="Staff "+uid,ClubUid=2,ClubReputation=3000,HasContract=true,ContractStart="2027-01-01",ContractEnd="2029-06-30",
        TeamId=100+uid,TeamSlot=slot,TeamName=squad,JobKey="coach",JobCode=2,
        Attributes=StaffRecruitment.Roles.SelectMany(p=>p.Weights.Keys).Concat(StaffRecruitment.Training.SelectMany(p=>p.Weights.Keys)).Distinct().ToDictionary(k=>k,k=>k=="working_with_youngsters"?youth:15)
    };
    public static void Run(Action<bool,string> check)
    {
        L10n.SetLanguage("it");
        var first=Person(1,null,0,18);var youth=Person(2,"U20",1,16);var low=Person(3,"U18",2,8);
        var unnamed=Person(4,null,1,17);var missing=Person(5,null,null);missing.Attributes.Remove("working_with_youngsters");
        var data=new StaffRecruitmentResult {Status="ready",ClubUid=1,ClubReputation=6000,GameDate="2028-03-28",Staff=[first,youth,low,unnamed,missing]};
        var coach=StaffRecruitment.Roles.Single(p=>p.Key=="coach");
        check(StaffSquads.AgeGroup(youth)=="u20" && StaffSquads.Matches(youth,"youth") && !StaffSquads.Matches(unnamed,"u20") && StaffSquads.Matches(unnamed,"subteam"),
            "readable youth age groups are distinct from unnamed subteam slots");
        var misleading=Person(6,"Club U200",1);check(StaffSquads.AgeGroup(misleading) is null,"squad age parsing requires complete age-group tokens");
        var ranks=StaffRecruitment.Rank(data,coach,targetSquad:"u20");
        check(ranks.Select(c=>c.Uid).OrderBy(i=>i).SequenceEqual(new uint[]{1,2,4}),"youth destination requires adequate readable working-with-youngsters without filtering current squad");
        check(ranks.Single(c=>c.Uid==1).Interest.Band==StaffInterest.Borderline && ranks.Single(c=>c.Uid==2).Interest.Band==StaffInterest.Likely,
            "first-team coach moving to a youth vacancy is not assumed to see a promotion");
        check(StaffRecruitment.Rank(data,coach,targetSquad:"u18",minimumYouthWork:7).Any(c=>c.Uid==3),"youth suitability minimum is adjustable");
        check(StaffRecruitment.Rank(data,coach,currentSquad:"u20").Single().Uid==2 && StaffRecruitment.Rank(data,coach,currentSquad:"unknown").Single().Uid==5,
            "current-squad filter uses only known contract facts including explicit unknown");
        var difficult=Person(7,"U20",1,20);difficult.ClubReputation=9000;data.Staff.Add(difficult);
        check(!StaffRecruitment.Rank(data,coach,targetSquad:"u20").Any(c=>c.Uid==7) &&
            StaffRecruitment.Rank(data,coach,ignoreInterest:true,targetSquad:"u20",currentSquad:"u20").Select(c=>c.Uid).OrderBy(i=>i).SequenceEqual(new uint[]{2,7}),
            "ignore-interest retains youth adequacy and current squad filtering");
        foreach(var key in new[]{"manager","assistant_manager","coach"})
        {
            var profile=StaffRecruitment.Roles.Single(p=>p.Key==key);
            check(StaffRecruitment.Rank(data,profile,targetSquad:"u18").All(c=>c.YouthWork>=12),"youth suitability applies to selected job "+key);
        }
        var training=StaffRecruitment.Training.First();
        check(StaffRecruitment.Rank(data,training,targetSquad:"u20").All(c=>c.RoleStars==CoachingStars.Calculate(c.Person).Single(r=>r.Category.Key==training.CategoryKey).Stars),
            "youth filtering does not alter training star formulas");
        var physio=StaffRecruitment.Roles.Single(p=>p.Key=="physio");
        check(StaffRecruitment.Rank(data,physio,targetSquad:"u20").Any(c=>c.Uid==5),"medical searches do not use coaching youth-work eligibility");
        using var host=new Form {Size=new Size(1000,800)};using var panel=new StaffRecruitmentPanel();host.Controls.Add(panel);
        panel.ClearForClub(1);panel.SetData(data);host.Show();Application.DoEvents();
        var target=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitTargetSquad");
        var current=All(panel).OfType<ComboBox>().Single(c=>c.Name=="RecruitCurrentSquad");
        var minimum=All(panel).OfType<NumericUpDown>().Single(c=>c.Name=="RecruitYouthMinimum");
        var grid=All(panel).OfType<DataGridView>().Single(c=>c.Name=="RecruitList");
        panel.ApplyPlanningSearch(coach,"coach",2000,"u20");Application.DoEvents();
        check(target.SelectedItem!.ToString()=="U20" && current.SelectedIndex==0 && minimum.Enabled && minimum.Value==12 && grid.Columns.Contains("YouthWork") && grid.Rows.Count==3,
            "recruitment UI supports youth vacancy with adjustable youth-work minimum and visible attribute");
        current.SelectedIndex=current.Items.Cast<object>().ToList().FindIndex(c=>c.ToString()=="U20");Application.DoEvents();
        check(grid.Rows.Count==1 && ((StaffCandidate)grid.Rows[0].DataBoundItem).Uid==2,"current-squad UI can narrow a youth search to existing U20 staff");
        L10n.SetLanguage("en");panel.SetLanguage();Application.DoEvents();
        check(target.SelectedItem!.ToString()=="U20" && current.SelectedItem!.ToString()=="U20" && grid.Rows.Count==1 && minimum.Value==12,
            "language switch preserves desired and current squad keys and youth minimum");
        host.Width=760;Application.DoEvents();
        check(grid.Height>60 && target.Visible && current.Visible,"staff squad search remains usable in narrow windows");
        host.Close();L10n.SetLanguage("it");
    }
}
