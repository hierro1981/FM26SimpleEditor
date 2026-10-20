namespace FM26SimpleEditor;

public enum SquadScope { FirstTeam, OtherTeams, All }
public sealed record SquadRecommendation(PlayerRow Player, double Score, int Familiarity)
{
    public double Stars => Math.Round(Score / 2, MidpointRounding.AwayFromZero) / 2;
    public IReadOnlyDictionary<string, double> PositionScores { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, int> Familiarities { get; init; } = new Dictionary<string, int>();
}

public static class SquadSelection
{
    public static IReadOnlyList<PlayerDetail> Members(MySquadResult data, ManagedSquad? club, SquadScope scope, uint? exactTeamId = null)
    {
        if (club is null || data.Status != "ready") return [];
        var ids = club.TeamIds.ToHashSet();
        return data.Players.Where(p => p.Player.TeamId is uint id && ids.Contains(id) &&
            (exactTeamId.HasValue ? id == exactTeamId.Value : scope == SquadScope.All || (scope == SquadScope.FirstTeam ? id == club.FirstTeamId : id != club.FirstTeamId)))
            .GroupBy(p => p.Player.Uid).Select(g => g.First()).ToArray();
    }

    public static IReadOnlyList<SquadRecommendation> TopFive(IEnumerable<PlayerDetail> members, params string[] positions)
        => TopFiveForRoles(members, positions.Select(p => new SquadTarget(p)).ToArray());

    public static IReadOnlyList<SquadRecommendation> TopFiveForRoles(IEnumerable<PlayerDetail> members, params SquadTarget[] targets)
    {
        var keys = targets.Distinct().ToArray();
        if (keys.Length is < 1 or > 2 || keys.Any(t => !PositionScheme.Order.Contains(t.Position) || !TacticalRoles.CanUse(t.RoleId, t.Position))) throw new ArgumentException(nameof(targets));
        var candidates = new List<SquadRecommendation>();
        foreach (var p in members.GroupBy(p => p.Player.Uid).Select(g => g.First()))
        {
            var profiles = keys.Select(target =>
            {
                var positions = p.Positions.Where(x => x.Key == target.Position).ToArray();
                int? familiarity = positions.Length == 1 && positions[0].Value is >= 1 and <= 20 ? positions[0].Value : null;
                return (Target: target, Score: TacticalRoles.Score(p, target), Familiarity: familiarity);
            }).ToArray();
            if (profiles.All(r => r.Score.HasValue && r.Familiarity is >= RoleComparison.PlayableMinimum))
            {
                candidates.Add(new(p.Player, profiles.Average(r => r.Score!.Value), profiles.Min(r => r.Familiarity!.Value))
                {
                    PositionScores = profiles.ToDictionary(r => r.Target.Key, r => r.Score!.Value),
                    Familiarities = profiles.GroupBy(r => r.Target.Position).ToDictionary(g => g.Key, g => g.Min(r => r.Familiarity!.Value))
                });
            }
        }
        return candidates.OrderByDescending(p => p.Score).ThenByDescending(p => p.Familiarity)
            .ThenBy(p => p.Player.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Player.Uid).Take(5).ToArray();
    }

    public static IOrderedEnumerable<PlayerRow> DefaultOrder(IEnumerable<PlayerRow> players) => players
        .OrderBy(p => PositionScheme.Rank(p.MainPosition)).ThenByDescending(p => p.Ca)
        .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Uid);
}
