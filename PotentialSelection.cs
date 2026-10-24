namespace FM26SimpleEditor;

public sealed record PotentialRecommendation(SquadRecommendation Current, double? Score,
    IReadOnlyDictionary<string, double> PositionScores, GrowthForecast? Growth = null)
{
    public PlayerRow Player => Current.Player;
    public double? Stars => Score.HasValue ? Math.Round(Score.Value / 2, MidpointRounding.AwayFromZero) / 2 : null;
    public int? Headroom => Growth is null ? null : Growth.Ability - Player.Ca;
}

public sealed record GrowthForecast(int Ability, int Conservative, int Favourable, bool Goalkeeper);

// Editor assumptions, NOT recovered FM rates or statistically calibrated forecasts.
public static class PotentialSelection
{
    private static readonly (double Age, double Rate)[] OutfieldCurve = [(15,16), (18,14), (21,10), (24,6), (27,2.5), (30,.8), (33,0)];
    private static readonly (double Age, double Rate)[] KeeperCurve = [(15,14), (18,13), (21,10), (24,7), (27,4), (30,2), (33,.6), (36,0)];

    public static GrowthForecast? Forecast(PlayerDetail detail)
    {
        var player = detail.Player;
        if (player.Ca is < 1 or > 200 || player.Pa is not (>= 1 and <= 200) || player.Age is not (>= 14 and <= 60)) return null;
        var positions = detail.Positions.Where(p => PositionScheme.Order.Contains(p.Key) && p.Value is >= 1 and <= 20)
            .GroupBy(p => p.Key).Where(g => g.Count() == 1).Select(g => g.Single()).ToArray();
        if (!positions.Any(p => p.Value >= 12)) return null;
        bool keeper = positions.Any(p => p.Key == "GK" && p.Value >= 12 && p.Value >= positions.Max(x => x.Value));
        var curve = keeper ? KeeperCurve : OutfieldCurve;
        int Project(double scenario)
        {
            double ability = player.Ca, ceiling = Math.Max(player.Ca, player.Pa.Value);
            // Half-year steps: age-tapered CA budget, also limited by the remaining PA gap.
            for (double age = player.Age.Value; age < curve[^1].Age; age += .5)
            {
                double at = Math.Max(curve[0].Age, age + .25);
                int i = 0;
                while (i < curve.Length - 2 && at > curve[i+1].Age) i++;
                double rate = curve[i].Rate + (curve[i+1].Rate - curve[i].Rate) *
                    Math.Clamp((at - curve[i].Age) / (curve[i+1].Age - curve[i].Age), 0, 1);
                ability += .5 * scenario * Math.Min(rate, .35 * (ceiling - ability));
            }
            return Math.Clamp((int)Math.Round(ability, MidpointRounding.AwayFromZero), player.Ca, (int)ceiling);
        }
        return new(Project(1), Project(.7), Project(1.3), keeper);
    }

    public static double? Factor(PlayerDetail player) => Forecast(player) is { } growth ? growth.Ability / (double)player.Player.Ca : null;

    public static double? Score(PlayerDetail player, SquadTarget target)
    {
        double? factor = Factor(player);
        if (factor is null) return null;
        var weights = TacticalRoles.Weights(target);
        var values = player.Attributes.GroupBy(a => a.Key).Where(g => g.Count() == 1 && g.First().Value is >= 1 and <= 125)
            .ToDictionary(g => g.Key, g => g.First().Value);
        if (weights.Count == 0 || weights.Keys.Any(key => !values.ContainsKey(key))) return null;
        return weights.Sum(w => Math.Min(20, PreviewMath.Attribute20(values[w.Key]) * factor.Value) * w.Value) / weights.Values.Sum();
    }

    public static IReadOnlyList<PotentialRecommendation> BestForRoles(IEnumerable<PlayerDetail> members, params SquadTarget[] targets)
    {
        var players = members.GroupBy(p => p.Player.Uid).Select(g => g.First()).ToDictionary(p => p.Player.Uid);
        var keys = targets.Distinct().ToArray();
        // Use the full eligible pool; projecting only today's top ten would miss young prospects.
        return SquadSelection.CandidatesForRoles(players.Values, targets).Select(current =>
        {
            var scores = keys.Select(t => (t.Key, Score: Score(players[current.Player.Uid], t))).ToArray();
            bool known = scores.All(s => s.Score.HasValue);
            return new PotentialRecommendation(current, known ? scores.Average(s => s.Score!.Value) : null,
                known ? scores.ToDictionary(s => s.Key, s => s.Score!.Value) : new Dictionary<string, double>(), Forecast(players[current.Player.Uid]));
        }).OrderByDescending(p => p.Score.HasValue).ThenByDescending(p => p.Score).ThenByDescending(p => p.Current.Score)
            .ThenByDescending(p => p.Current.Familiarity).ThenBy(p => p.Player.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Player.Uid)
            .Take(Math.Clamp(SquadSelection.ResultLimit, 1, 100)).ToArray();
    }
}
