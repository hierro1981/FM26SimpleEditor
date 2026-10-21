namespace FM26SimpleEditor;

public sealed record CoachingCategory(string Key, string Primary, string? Secondary = null)
{
    public IReadOnlyDictionary<string, int> Weights => Secondary is null
        ? new Dictionary<string, int> { [Primary] = 9, ["determination"] = 2, ["discipline"] = 2, ["motivating"] = 2 }
        : new Dictionary<string, int> { [Primary] = 6, [Secondary] = 3, ["determination"] = 2, ["discipline"] = 2, ["motivating"] = 2 };
}
public sealed record CoachingRating(CoachingCategory Category, int? Score, IReadOnlyList<string> Missing)
{
    public double? Stars => Score is int value ? CoachingStars.FromScore(value) : null;
}

// An explicit attribute-based estimate, not a decoded FM26 star function.
// See docs/CoachingStars.md, including the observed Genie Scout discrepancy.
public static class CoachingStars
{
    public const string HistoricalSource = "https://community.sports-interactive.com/forums/topic/439305-coaches-and-their-star-ratings/";
    public static IReadOnlyList<CoachingCategory> Categories { get; } = Array.AsReadOnly<CoachingCategory>([
        new("AttackTactical", "coaching_attacking", "coaching_tactical"),
        new("AttackTechnical", "coaching_attacking", "coaching_technical"),
        new("DefenceTactical", "coaching_defending", "coaching_tactical"),
        new("DefenceTechnical", "coaching_defending", "coaching_technical"),
        new("PossessionTactical", "coaching_possession", "coaching_tactical"),
        new("PossessionTechnical", "coaching_possession", "coaching_technical"),
        new("Goalkeeping", "coaching_goalkeeping"),
        new("Fitness", "coaching_fitness"),
        new("SetPieces", "coaching_set_pieces")
    ]);
    public static double FromScore(int score)
    {
        if (score is < 0 or > 300) throw new ArgumentOutOfRangeException(nameof(score));
        return Math.Min(5, .5 + score / 30 * .5);
    }
    public static IReadOnlyList<CoachingRating> Calculate(StaffRow person) => Categories.Select(category =>
    {
        var missing = category.Weights.Keys.Where(key => !person.Attributes.TryGetValue(key, out int value) || value is < 1 or > 20).ToArray();
        int? score = !person.IsHumanManager && missing.Length == 0 ? category.Weights.Sum(w => person.Attributes[w.Key] * w.Value) : null;
        return new CoachingRating(category, score, missing);
    }).ToArray();
    public static string Calculation(StaffRow person, CoachingRating rating) => string.Join(" + ", rating.Category.Weights.Select(w =>
        w.Value + " × " + L10n.Get("StaffAttribute_" + w.Key) + " (" +
        (person.Attributes.TryGetValue(w.Key, out int value) && value is >= 1 and <= 20 ? value.ToString(L10n.Culture) : "—") + ")"));
}
