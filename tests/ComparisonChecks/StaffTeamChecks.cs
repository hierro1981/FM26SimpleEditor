using FM26SimpleEditor;

internal static class StaffTeamChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] {child}.Concat(All(child)));
    public static void Run(Action<bool,string> check)
    {
        L10n.SetLanguage("it");
        var first = new StaffRow { Uid=1,Name="First",ClubUid=2,HasContract=true,TeamId=100,TeamSlot=0,JobCode=48,JobKey="sports_scientist",ClubReputation=3000,
            ContractStart="2027-01-01",ContractEnd="2029-06-30",Attributes=new(){["sports_science"]=18,["analysing_data"]=10} };
        var youth = new StaffRow { Uid=2,Name="Youth",ClubUid=2,HasContract=true,TeamId=101,TeamSlot=1,TeamName="U20",JobCode=48,JobKey="sports_scientist",ClubReputation=3000,
            ContractStart="2027-01-01",ContractEnd="2029-06-30",Attributes=new(){["sports_science"]=18,["analysing_data"]=10} };
        var unknownSub = new StaffRow { HasContract=true,TeamId=102,TeamSlot=2 };
        var affiliate = new StaffRow { HasContract=true,TeamId=900,TeamSlot=0,ClubUid=2,TeamClubUid=3,TeamClubName="Club B" };
        var reserves = new StaffRow {HasContract=true,TeamId=120,TeamSlot=3,TeamName="Altra squadra",TeamTypeCode=22,TeamAgeLimit=100,TeamTypeMethod="unclassified_subteam"};
        check(reserves.TeamDisplay=="Altra squadra" && reserves.TeamTooltip.Contains("22"), "unclassified additional squad exposes its type evidence");
        var unknown = new StaffRow { HasContract=true,TeamId=999 };
        check(first.TeamDisplay=="Prima squadra" && youth.TeamDisplay=="U20", "staff contract squad uses first-team convention or a readable squad name");
        check(unknownSub.TeamDisplay.Contains("slot 2") && !unknownSub.TeamDisplay.Contains("U18") && unknown.TeamDisplay.Contains("999"),
            "unknown subteam ages are not inferred from slot order and unmatched teams retain their ID");
        check(affiliate.TeamDisplay=="Club B · Prima squadra" && !new StaffRow {TeamId=100,TeamSlot=0}.TeamDisplay.Contains("Prima"),
            "affiliate owner is distinguished and an unreadable contract never implies a first-team assignment");
        check(youth.TeamTooltip.Contains("101") && youth.TeamTooltip.Contains("lo slot da solo"), "staff squad tooltip exposes provenance and unmapped age-group limits");
        var director=new StaffRow { Uid=1000,ClubUid=2,HasContract=false,TeamId=100,TeamSlot=0,TeamAssignmentSource="role",JobCode=6,JobKey="director" };
        check(director.HasTeamAssignment && director.TeamDisplay=="Prima squadra" && StaffSquads.Matches(director,"first") &&
            !StaffSquads.Matches(director,"unknown") && director.RoleDisplay=="Dirigente" && director.Wage is null,
            "FM27 unpaid director retains explicit role/team evidence without inventing salary or a paid contract");
        var chiefAnalyst=new StaffRow { JobKey="analyst",JobTitleKey="head_performance_analyst",JobCode=62,JobFamilyOnly=false,
            Attributes=new(){["analysing_data"]=15,["tactical_knowledge"]=14,["judging_ability"]=13} };
        check(chiefAnalyst.RoleDisplay=="Capo analista delle performance" && StaffRecruitment.AnalystJob(chiefAnalyst)==62 &&
            StaffRecruitment.CurrentProfile(chiefAnalyst)?.Key=="analyst" && !chiefAnalyst.RoleTooltip.Contains(L10n.Get("AnalystCategoryNote")),
            "FM27 chief analyst has the verified title and retains the shared scoring/variant filter");
        var data = new StaffRecruitmentResult {Status="ready",ClubUid=1,ClubReputation=6000,GameDate="2028-03-28",Staff=[first,youth]};
        var profile=StaffRecruitment.Roles.Single(p=>p.Key=="sports_scientist");
        check(StaffRecruitment.Rank(data,profile,search:"U20").Single().Uid==2 && StaffRecruitment.Rank(data,profile,search:"101").Single().Uid==2,
            "staff recruitment searches readable squad names and contract team IDs");
        using var host=new Form {Size=new Size(1000,750)};using var panel=new StaffPanel();host.Controls.Add(panel);
        panel.ClearForClub(2);panel.SetData(new(){Status="ready",ClubUid=2,Staff=[first,youth]});host.Show();Application.DoEvents();
        var grid=All(panel).OfType<DataGridView>().Single(g=>g.Name=="StaffList");
        check(grid.Columns["TeamDisplay"].SortMode==DataGridViewColumnSortMode.Programmatic && grid.Rows.Cast<DataGridViewRow>().Any(r=>r.Cells["TeamDisplay"].Value?.ToString()=="U20"),
            "own-staff list displays a sortable squad column");
        var search=All(panel).OfType<TextBox>().Single(t=>t.Name=="StaffSearch");search.Text="U20";
        check(grid.Rows.Count==1 && ((StaffRow)grid.Rows[0].DataBoundItem).Uid==2,"own-staff search includes squad names");
        L10n.SetLanguage("en");panel.SetLanguage();Application.DoEvents();
        check(first.TeamDisplay=="First team" && reserves.TeamDisplay=="Additional squad" && unknownSub.TeamDisplay.Contains("Subteam") && youth.TeamDisplay=="U20" && grid.Columns["TeamDisplay"].HeaderText=="Job squad",
            "squad language switches translate labels without translating actual team names");
        host.Close();L10n.SetLanguage("it");
    }
}
