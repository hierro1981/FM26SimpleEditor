using FM26SimpleEditor;

internal static class PotentialSelectionChecks
{
    private static PlayerDetail Player(uint uid, int ca, int? pa, int raw, int familiarity = 20) => new()
    {
        Player = new() { Uid = uid, Name = "P" + uid, Ca = ca, Pa = pa, Age = 17 },
        Attributes = TacticalRoles.All.SelectMany(r => r.Focus.Concat(r.Support)).Concat(PositionScheme.Order.SelectMany(RoleImportance.Fields).Select(f => f.Key)).Distinct()
            .Select((key,i) => new EditableField { Key = key, Index = i, Value = raw }).ToList(),
        Positions = [new() { Key = "MC", Value = familiarity }, new() { Key = "DM", Value = familiarity }]
    };
    public static void Run(Action<bool,string> check)
    {
        var ip = new SquadTarget("MC", "ip_midfield-playmaker");
        var op = new SquadTarget("DM", "op_defensive-midfielder");
        var veteran = Player(1,140,140,85); var youngster = Player(2,80,160,50); var unknown = Player(3,100,null,75);
        int previous = SquadSelection.ResultLimit;
        try
        {
            SquadSelection.ResultLimit = 1;
            check(SquadSelection.BestForRoles([veteran,youngster],ip).Single().Player.Uid == 1 && PotentialSelection.BestForRoles([veteran,youngster],ip).Single().Player.Uid == 2,
                "future ranking finds a low-CA high-PA prospect outside today's limited shortlist");
            SquadSelection.ResultLimit = 10;
            var rows = PotentialSelection.BestForRoles([veteran,youngster,unknown],ip,op);
            check(rows[0].Player.Uid == 2 && rows[0].Score == 20 && rows[0].Stars == 5 && rows[0].Headroom == 80 && rows[0].PositionScores.Count == 2,
                "potential scenario applies both real tactic profiles and shared absolute role stars");
            check(rows.Last().Player.Uid == 3 && rows.Last().Score is null && rows.Last().Stars is null && rows.Last().Headroom is null,
                "unknown PA remains visible with missing projections rather than an invented ceiling");
            check(rows.Single(r=>r.Player.Uid==1).Score == rows.Single(r=>r.Player.Uid==1).Current.Score,
                "PA equals CA preserves current role score exactly");
            var peaked = Player(4,160,120,80);
            check(PotentialSelection.Score(peaked,ip) == TacticalRoles.Score(peaked,ip) && PotentialSelection.BestForRoles([peaked],ip).Single().Headroom == 0,
                "PA below CA neither invents growth nor predicts decline");
            var uneven = Player(5,100,200,25); var weights = TacticalRoles.Weights(ip); string high = weights.Keys.First();
            uneven.Attributes.Single(a=>a.Key==high).Value=100;
            double factor = PotentialSelection.Factor(uneven)!.Value;
            double expected = weights.Sum(w=>(w.Key==high ? 20.0 : 5.0*factor)*w.Value)/weights.Values.Sum();
            check(Math.Abs(PotentialSelection.Score(uneven,ip)!.Value - expected)<1e-9 && PotentialSelection.Score(uneven,ip) < Math.Min(20,TacticalRoles.Score(uneven,ip)!.Value*factor),
                "each projected attribute is capped before role weighting rather than capping the aggregate score");
            var original = string.Join(",",youngster.Attributes.Select(a=>a.Value));
            PotentialSelection.BestForRoles([youngster,youngster],ip,op);
            check(string.Join(",",youngster.Attributes.Select(a=>a.Value)) == original && youngster.Player.Ca==80 && youngster.Player.Pa==160 && youngster.Positions.All(p=>p.Value==20),
                "projection does not mutate real attributes, abilities or position familiarity");
            check(PotentialSelection.BestForRoles([Player(6,80,200,75,10)],ip).Count==0,
                "high PA never bypasses today's position familiarity requirement");
            foreach(var p in new[] {Player(7,0,160,75),Player(7,100,201,75),Player(7,201,200,75),Player(7,100,-7,75)})
                check(PotentialSelection.Score(p,ip) is null, "invalid or coded CA/PA does not produce a projection " + p.Player.Ca + "/" + p.Player.Pa);
            var missing=Player(8,100,200,75);missing.Attributes.RemoveAll(a=>a.Key==high);
            check(PotentialSelection.BestForRoles([missing],ip).Count==0, "missing current role attributes do not become fictional future data");
            var young = Player(9,60,200,40); young.Player.Age=18;
            var old = Player(10,60,200,40); old.Player.Age=34;
            var growth = PotentialSelection.Forecast(young)!;
            check(growth.Ability==139 && growth.Conservative==115 && growth.Favourable==163,
                "18-year-old 60/200 has an explicit age-limited forecast rather than automatic PA");
            check(PotentialSelection.Forecast(old)!.Ability==60 && PotentialSelection.Score(old,ip)==TacticalRoles.Score(old,ip),
                "34-year-old outfield 60/200 does not get fictitious massive future growth");
            check(growth.Conservative<=growth.Ability && growth.Ability<=growth.Favourable && growth.Favourable<=200,
                "scenario bounds surround the central forecast and respect PA");
            int last=200;
            for(int age=15;age<=40;age++)
            {
                young.Player.Age=age; int ability=PotentialSelection.Forecast(young)!.Ability;
                check(ability<=last && ability>=60, "remaining growth never increases with age or drops below today's CA: " + age);
                last=ability;
            }
            young.Player.Age=28; var keeper=Player(11,60,200,40); keeper.Player.Age=28;
            keeper.Positions=[new(){Key="GK",Value=20}];
            check(PotentialSelection.Forecast(keeper) is { Goalkeeper:true } kg && kg.Ability>PotentialSelection.Forecast(young)!.Ability,
                "established goalkeepers use a distinct longer remaining-growth curve");
            keeper.Positions.Add(new(){Key="MC",Value=20}); keeper.Positions[0].Value=12;
            check(PotentialSelection.Forecast(keeper) is {Goalkeeper:false}, "secondary goalkeeper familiarity does not grant the goalkeeper curve");
            keeper.Player.Age=36;keeper.Positions[0].Value=20;
            check(PotentialSelection.Forecast(keeper)!.Ability==60, "goalkeepers past their growth window keep current CA without assumed future gains");
            foreach(int? age in new int?[]{null,-1,0,13,61})
            {
                young.Player.Age=age;
                check(PotentialSelection.Forecast(young) is null && PotentialSelection.Score(young,ip) is null,
                    "missing or invalid age is not replaced with an optimistic youth age: " + age);
            }
            young.Player.Age=18; young.Positions.Clear();
            check(PotentialSelection.Forecast(young) is null, "unknown playing position does not silently choose a growth curve");
            var ageMissing=Player(12,100,200,75);ageMissing.Player.Age=null;
            check(PotentialSelection.BestForRoles([ageMissing],ip).Single() is {Score:null,Growth:null,Headroom:null},
                "eligible current player with missing age remains visible without fabricated future values");
        }
        finally { SquadSelection.ResultLimit=previous; }
    }
}
