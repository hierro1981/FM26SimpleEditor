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
    {
        var keys = positions.Distinct().ToArray();
        if (keys.Length is < 1 or > 2 || keys.Any(key => !PositionScheme.Order.Contains(key))) throw new ArgumentException(nameof(positions));
        var candidates = new List<SquadRecommendation>();
        foreach (var p in members.GroupBy(p => p.Player.Uid).Select(g => g.First()))
        {
            var profiles = keys.Select(key => RoleComparison.Compare(p, p, key)).ToArray();
            if (profiles.All(r => r.Complete && r.ScoreA.HasValue && r.FamiliarityA is >= RoleComparison.PlayableMinimum))
            {
                // Each position counts equally; competence is required in BOTH.
                candidates.Add(new(p.Player, profiles.Average(r => r.ScoreA!.Value), profiles.Min(r => r.FamiliarityA!.Value))
                {
                    PositionScores = profiles.ToDictionary(r => r.Position, r => r.ScoreA!.Value),
                    Familiarities = profiles.ToDictionary(r => r.Position, r => r.FamiliarityA!.Value)
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
