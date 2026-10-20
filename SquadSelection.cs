namespace FM26SimpleEditor;

public enum SquadScope { FirstTeam, OtherTeams, All }
public sealed record SquadRecommendation(PlayerRow Player, double Score, int Familiarity);

public static class SquadSelection
{
    public static IReadOnlyList<PlayerDetail> Members(MySquadResult data, ManagedSquad? club, SquadScope scope)
    {
        if (club is null || data.Status != "ready") return [];
        var ids = club.TeamIds.ToHashSet();
        return data.Players.Where(p => p.Player.TeamId is uint id && ids.Contains(id) &&
            (scope == SquadScope.All || (scope == SquadScope.FirstTeam ? id == club.FirstTeamId : id != club.FirstTeamId)))
            .GroupBy(p => p.Player.Uid).Select(g => g.First()).ToArray();
    }

    public static IReadOnlyList<SquadRecommendation> TopThree(IEnumerable<PlayerDetail> members, string position)
    {
        if (!PositionScheme.Order.Contains(position)) throw new ArgumentException(nameof(position));
        var candidates = new List<SquadRecommendation>();
        foreach (var p in members)
        {
            var result = RoleComparison.Compare(p, p, position);
            if (result.Complete && result.ScoreA is double score && result.FamiliarityA is >= RoleComparison.PlayableMinimum)
                candidates.Add(new(p.Player, score, result.FamiliarityA.Value));
        }
        // Stable, explicit tie-breakers; familiarity never silently changes the score.
        return candidates.OrderByDescending(p => p.Score).ThenByDescending(p => p.Familiarity)
            .ThenBy(p => p.Player.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Player.Uid).Take(3).ToArray();
    }
}
