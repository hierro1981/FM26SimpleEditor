namespace FM26SimpleEditor;

public sealed record PotentialRecommendation(SquadRecommendation Current, double? Score,
    IReadOnlyDictionary<string, double> PositionScores)
{
    public PlayerRow Player => Current.Player;
    public double? Stars => Score.HasValue ? Math.Round(Score.Value / 2, MidpointRounding.AwayFromZero) / 2 : null;
    public int? Headroom => PotentialSelection.Factor(Player) is null ? null : Math.Max(0, Player.Pa!.Value - Player.Ca);
}

// A conditional scenario, not a recovered FM attribute-development formula.
// Keep the current attribute distribution, scale by PA/CA and cap each input
// before applying the existing role weights. Never mutate the source profile.
public static class PotentialSelection
{
    public static double? Factor(PlayerRow player) => player.Ca is >= 1 and <= 200 && player.Pa is >= 1 and <= 200
        ? Math.Max(1, player.Pa.Value / (double)player.Ca) : null;

    public static double? Score(PlayerDetail player, SquadTarget target)
    {
        double? factor = Factor(player.Player);
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
                known ? scores.ToDictionary(s => s.Key, s => s.Score!.Value) : new Dictionary<string, double>());
        }).OrderByDescending(p => p.Score.HasValue).ThenByDescending(p => p.Score).ThenByDescending(p => p.Current.Score)
            .ThenByDescending(p => p.Current.Familiarity).ThenBy(p => p.Player.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Player.Uid)
            .Take(Math.Clamp(SquadSelection.ResultLimit, 1, 100)).ToArray();
    }
}
