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
            double expected = weights.Sum(w=>(w.Key==high ? 20.0 : 10.0)*w.Value)/weights.Values.Sum();
            check(Math.Abs(PotentialSelection.Score(uneven,ip)!.Value - expected)<1e-9 && PotentialSelection.Score(uneven,ip) < Math.Min(20,TacticalRoles.Score(uneven,ip)!.Value*2),
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
        }
        finally { SquadSelection.ResultLimit=previous; }
    }
}
