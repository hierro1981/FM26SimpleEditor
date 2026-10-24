namespace FM26SimpleEditor;

public enum LineupGap { None, UnknownRole, NoCandidate, UsedElsewhere }
public sealed record LineupSlot(SavedTacticSlot Slot, SquadRecommendation? Recommendation, LineupGap Gap);
public sealed record LineupResult(IReadOnlyList<LineupSlot> Slots, bool Future = false)
{
    public int Filled => Slots.Count(s => s.Recommendation is not null);
    public double Total => Slots.Sum(s => s.Recommendation?.Score ?? 0);
    public double? Average => Filled == 0 ? null : Total / Filled;
}

public static class LineupSelection
{
    /// <summary>
    /// Maximum-weight rectangular assignment. Fill as many eligible slots as
    /// possible, then maximise the sum of the existing tactical-role scores.
    /// Dummy columns leave gaps explicit; players are never reused or guessed.
    /// </summary>
    public static LineupResult ForTactic(IEnumerable<PlayerDetail> members, SavedTactic? tactic, string phase, bool future = false)
    {
        if (tactic is null) return new([], future);
        var slots = tactic.Slots.OrderBy(s => s.Number).ToArray();
        if (slots.Length > 11 || slots.Select(s => s.Number).Distinct().Count() != slots.Length)
            throw new ArgumentException("A tactic must have at most eleven distinct slots.", nameof(tactic));
        var players = members.GroupBy(p => p.Player.Uid).Select(g => g.First()).OrderBy(p => p.Player.Uid).ToArray();
        var candidates = slots.Select(slot =>
        {
            var targets = TacticSuggestions.Targets(slot, phase);
            if (targets.Length == 0) return new Dictionary<uint, SquadRecommendation>();
            return future
                ? PotentialSelection.CandidatesForRoles(players, targets).Where(p => p.Score.HasValue)
                    .ToDictionary(p => p.Player.Uid, p => p.Current with { Score = p.Score!.Value, PositionScores = p.PositionScores })
                : SquadSelection.CandidatesForRoles(players, targets).ToDictionary(r => r.Player.Uid);
        }).ToArray();
        int rows = slots.Length, columns = players.Length + rows;
        if (rows == 0) return new([], future);
        // Scores are 1..20, with at most eleven slots. A 1000 bonus therefore
        // prioritises coverage without changing the optimum at equal coverage.
        double Cost(int row, int column) => column >= players.Length ? 0 :
            candidates[row].TryGetValue(players[column].Player.Uid, out var r) ? -1000 - r.Score : 1_000_000;
        var u = new double[rows + 1]; var v = new double[columns + 1];
        var matchedRow = new int[columns + 1]; var previous = new int[columns + 1];
        for (int row = 1; row <= rows; row++)
        {
            matchedRow[0] = row;
            var minimum = Enumerable.Repeat(double.PositiveInfinity, columns + 1).ToArray();
            var used = new bool[columns + 1]; int column = 0;
            do
            {
                used[column] = true; int currentRow = matchedRow[column], next = 0;
                double delta = double.PositiveInfinity;
                for (int j = 1; j <= columns; j++)
                {
                    if (used[j]) continue;
                    double cost = Cost(currentRow - 1, j - 1) - u[currentRow] - v[j];
                    if (cost < minimum[j]) { minimum[j] = cost; previous[j] = column; }
                    // Stable UID/slot ordering makes equal optima repeatable.
                    if (minimum[j] < delta) { delta = minimum[j]; next = j; }
                }
                for (int j = 0; j <= columns; j++)
                    if (used[j]) { u[matchedRow[j]] += delta; v[j] -= delta; }
                    else minimum[j] -= delta;
                column = next;
            } while (matchedRow[column] != 0);
            do { int next = previous[column]; matchedRow[column] = matchedRow[next]; column = next; } while (column != 0);
        }
        var assigned = new SquadRecommendation?[rows];
        for (int j = 1; j <= players.Length; j++)
            if (matchedRow[j] > 0 && candidates[matchedRow[j] - 1].TryGetValue(players[j - 1].Player.Uid, out var r))
                assigned[matchedRow[j] - 1] = r;
        return new(slots.Select((slot, i) => new LineupSlot(slot, assigned[i], assigned[i] is not null ? LineupGap.None :
            TacticSuggestions.Targets(slot, phase).Length == 0 ? LineupGap.UnknownRole :
            candidates[i].Count == 0 ? LineupGap.NoCandidate : LineupGap.UsedElsewhere)).ToArray(), future);
    }
}
