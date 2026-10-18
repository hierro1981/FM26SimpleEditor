namespace FM26SimpleEditor;

public enum ComparisonSide { None, A, B }
public enum RoleVerdict { Incomplete, UnknownFamiliarity, Unfamiliar, OnlyA, OnlyB, AttributesA, AttributesB, FamiliarityA, FamiliarityB, Balanced }

public sealed record RoleComparisonStat(string Key, int Tier, int A, int B)
{
    public int Weight => 4 - Tier;
    public int Delta => A - B;
}

public sealed record RoleComparisonResult(string Position, double? ScoreA, double? ScoreB,
    int? FamiliarityA, int? FamiliarityB, IReadOnlyList<RoleComparisonStat> Stats,
    IReadOnlyList<string> Missing, RoleVerdict Verdict, ComparisonSide Suggested)
{
    public bool Complete => Missing.Count == 0;
    public int Expected => Stats.Count + Missing.Count;
}

// Transparent editor heuristic, independent of CA/PA, star ratings and FM match formulas.
public static class RoleComparison
{
    public const double BalancedMargin = .25;
    public const int PlayableMinimum = 12;

    public static RoleComparisonResult Compare(PlayerDetail a, PlayerDetail b, string position)
    {
        if (!PositionScheme.Order.Contains(position)) throw new ArgumentException("Posizione non riconosciuta", nameof(position));
        static Dictionary<string, int> Attributes(PlayerDetail player) => player.Attributes.GroupBy(f => f.Key)
            .Where(g => g.Count() == 1 && g.First().Value is >= 1 and <= 125)
            .ToDictionary(g => g.Key, g => g.First().Value);
        static int? Familiarity(PlayerDetail player, string key)
        {
            var matches = player.Positions.Where(p => p.Key == key).ToArray();
            return matches.Length == 1 && matches[0].Value is >= 1 and <= 20 ? matches[0].Value : null;
        }
        var left = Attributes(a); var right = Attributes(b);
        var stats = new List<RoleComparisonStat>(); var missing = new List<string>();
        foreach (var (key, tier) in RoleImportance.Fields(position))
        {
            if (left.TryGetValue(key, out int av) && right.TryGetValue(key, out int bv))
                stats.Add(new(key, tier, PreviewMath.Attribute20(av), PreviewMath.Attribute20(bv)));
            else missing.Add(key);
        }
        int totalWeight = stats.Sum(s => s.Weight);
        double? scoreA = totalWeight > 0 ? stats.Sum(s => s.A * s.Weight) / (double)totalWeight : null;
        double? scoreB = totalWeight > 0 ? stats.Sum(s => s.B * s.Weight) / (double)totalWeight : null;
        int? fa = Familiarity(a, position), fb = Familiarity(b, position);
        var verdict = RoleVerdict.Incomplete;
        var suggested = ComparisonSide.None;
        if (missing.Count == 0 && scoreA.HasValue && scoreB.HasValue)
        {
            if (fa is null || fb is null) verdict = RoleVerdict.UnknownFamiliarity;
            else if (fa < PlayableMinimum && fb < PlayableMinimum) verdict = RoleVerdict.Unfamiliar;
            else if (fa >= PlayableMinimum && fb < PlayableMinimum) { verdict = RoleVerdict.OnlyA; suggested = ComparisonSide.A; }
            else if (fb >= PlayableMinimum && fa < PlayableMinimum) { verdict = RoleVerdict.OnlyB; suggested = ComparisonSide.B; }
            else if (Math.Abs(scoreA.Value - scoreB.Value) < BalancedMargin)
            {
                if (fa - fb >= 3) { verdict = RoleVerdict.FamiliarityA; suggested = ComparisonSide.A; }
                else if (fb - fa >= 3) { verdict = RoleVerdict.FamiliarityB; suggested = ComparisonSide.B; }
                else verdict = RoleVerdict.Balanced;
            }
            else if (scoreA > scoreB) { verdict = RoleVerdict.AttributesA; suggested = ComparisonSide.A; }
            else { verdict = RoleVerdict.AttributesB; suggested = ComparisonSide.B; }
        }
        return new(position, scoreA, scoreB, fa, fb, stats, missing, verdict, suggested);
    }
}
