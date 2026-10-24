using System.Globalization;

namespace FM26SimpleEditor;

public sealed record StaffSearchProfile(string Key, IReadOnlyDictionary<string, int> Weights, string? CategoryKey = null)
{
    public string Label => CategoryKey is null ? L10n.Get("StaffJob_" + Key) : L10n.Get("CoachingCategory_" + CategoryKey);
    public override string ToString() => Label;
}
public enum StaffInterest { Likely, Borderline, Uncertain, Unlikely }
public sealed record StaffInterestEstimate(StaffInterest Band, int Score, IReadOnlyList<string> Reasons)
{
    public string Label => L10n.Get("RecruitInterest_" + Band);
}
public sealed record StaffCandidate(StaffRow Person, StaffSearchProfile Profile, double? Quality,
    StaffInterestEstimate Interest, CoachingRating? BestTraining)
{
    public string Name => Person.Name;
    public uint Uid => Person.Uid;
    public uint? PublicId => Person.Id;
    public string Club => Person.ClubName;
    public int? ClubReputation => Person.ClubReputation is >= 1 and <= 10000 ? Person.ClubReputation : null;
    public string CurrentRole => Person.RoleDisplay;
    public int? Ca => Person.Ca;
    public int? Age => Person.Age;
    public long? Wage => Person.Wage;
    public double? OverallStars => Person.OverallStars;
    public double? RoleStars => Profile.CategoryKey is string key ? CoachingStars.Calculate(Person).Single(c => c.Category.Key == key).Stars :
        Quality is double q ? Math.Clamp(Math.Round(q / 4 * 2, MidpointRounding.AwayFromZero) / 2, .5, 5) : null;
    public double? TrainingStars => BestTraining?.Stars;
    public string TrainingCategory => BestTraining is null ? "—" : L10n.Get("CoachingCategory_" + BestTraining.Category.Key);
    public string InterestLabel => Interest.Label;
    public string MainAttributes => string.Join(" · ", Profile.Weights.OrderByDescending(w => w.Value).Select(w =>
        L10n.Get("StaffAttribute_" + w.Key) + ": " + (StaffRecruitment.Attribute(Person, w.Key)?.ToString(L10n.Culture) ?? "—")));
}

// Transparent shortlist heuristics. No official FM acceptance or job-rating formula is claimed.
public static class StaffRecruitment
{
    private static StaffSearchProfile Role(string key, params (string Key, int Weight)[] fields) => new(key, fields.ToDictionary(f => f.Key, f => f.Weight));
    public static IReadOnlyList<StaffSearchProfile> Roles { get; } = Array.AsReadOnly<StaffSearchProfile>([
        Role("manager", ("tactical_knowledge",3), ("man_management",3), ("judging_ability",2), ("motivating",2)),
        Role("assistant_manager", ("judging_ability",3), ("tactical_knowledge",3), ("man_management",2), ("motivating",2)),
        Role("coach", ("coaching",3), ("determination",2), ("discipline",2), ("motivating",2)),
        Role("goalkeeping_coach", ("coaching_goalkeeping",6), ("determination",2), ("discipline",2), ("motivating",2)),
        Role("fitness_coach", ("coaching_fitness",6), ("determination",2), ("discipline",2), ("motivating",2)),
        Role("set_piece_coach", ("coaching_set_pieces",6), ("coaching_tactical",3), ("determination",2), ("discipline",2), ("motivating",2)),
        Role("head_youth_development", ("working_with_youngsters",4), ("judging_potential",3), ("judging_ability",2), ("man_management",1)),
        Role("head_player_development", ("working_with_youngsters",3), ("judging_potential",3), ("man_management",2), ("judging_ability",2)),
        Role("director_of_football", ("negotiating",4), ("judging_ability",3), ("judging_potential",3)),
        Role("technical_director", ("judging_staff_ability",4), ("negotiating",3), ("man_management",3)),
        Role("scout", ("judging_ability",4), ("judging_potential",4), ("adaptability",2)),
        Role("chief_scout", ("judging_ability",3), ("judging_potential",3), ("man_management",2), ("adaptability",2)),
        Role("analyst", ("analysing_data",6), ("tactical_knowledge",3), ("judging_ability",1)),
        Role("physio", ("physiotherapy",8), ("man_management",2)),
        Role("head_physio", ("physiotherapy",6), ("man_management",4)),
        Role("sports_scientist", ("sports_science",8), ("analysing_data",2)),
        Role("head_sports_science", ("sports_science",6), ("man_management",3), ("analysing_data",1)),
        // No verified doctor-rating input: expose known doctors without fabricated quality stars.
        Role("doctor")
    ]);
    public static IReadOnlyList<StaffSearchProfile> Training { get; } = CoachingStars.Categories.Select(c => new StaffSearchProfile(c.Key, c.Weights, c.Key)).ToArray();
    public static int? Attribute(StaffRow person, string key) => person.Attributes.TryGetValue(key, out int n) && n is >= 1 and <= 20 ? n : null;
    public static double? Quality(StaffRow person, StaffSearchProfile profile)
    {
        if (person.IsHumanManager || profile.Weights.Count == 0) return null;
        if (profile.CategoryKey is string key) return CoachingStars.Calculate(person).Single(c => c.Category.Key == key).Score / 15.0;
        if (profile.Weights.Keys.Any(k => Attribute(person, k) is null)) return null;
        return profile.Weights.Sum(w => Attribute(person, w.Key)!.Value * w.Value) / (double)profile.Weights.Values.Sum();
    }
    private static DateTime? Date(string? value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
    private static bool Reputation(int? value) => value is >= 1 and <= 10000;
    public static StaffInterestEstimate Estimate(StaffRow person, StaffSearchProfile profile, StaffRecruitmentResult destination, long offeredWage = 0)
    {
        var reasons = new List<string>(); int score = 0;
        var clock = Date(destination.GameDate); var end = Date(person.ContractEnd); var start = Date(person.ContractStart);
        bool expired = person.HasContract && clock.HasValue && end.HasValue && end < clock;
        bool current = person.HasContract && !expired && (!start.HasValue || !clock.HasValue || start <= clock);
        bool repKnown = current && Reputation(person.ClubReputation) && Reputation(destination.ClubReputation);
        if (repKnown)
        {
            int delta = destination.ClubReputation!.Value - person.ClubReputation!.Value;
            score += (int)Math.Round(delta * .006);
            reasons.Add(L10n.Format("RecruitRepComparison", destination.ClubReputation, person.ClubReputation));
            if (person.Personality.TryGetValue("ambition", out int ambition) && ambition is >= 1 and <= 20 && Math.Abs(delta) >= 1000)
            { score += Math.Sign(delta) * (ambition - 1) / 4; reasons.Add(L10n.Format("RecruitAmbition", ambition)); }
        }
        else reasons.Add(L10n.Get("RecruitRepUnknown"));
        if (expired) { score += 12; reasons.Add(L10n.Get("RecruitExpired")); }
        else if (current && end.HasValue && clock.HasValue && (end.Value-clock.Value).TotalDays <= 180)
        { score += 8; reasons.Add(L10n.Get("RecruitExpiring")); }
        if (!person.HasContract) reasons.Add(L10n.Get("RecruitContractUnknown"));
        if (start > clock) reasons.Add(L10n.Get("RecruitFutureContract"));
        if (current && person.Personality.TryGetValue("loyalty", out int loyalty) && loyalty is >= 1 and <= 20)
        { score -= (loyalty - 1) / 4; reasons.Add(L10n.Format("RecruitLoyalty", loyalty)); }
        if (offeredWage > 0 && current && person.Wage is > 0)
        {
            double ratio = offeredWage / (double)person.Wage;
            score += ratio < .75 ? -30 : ratio < 1 ? -12 : ratio >= 1.5 ? 15 : ratio >= 1.1 ? 6 : 0;
            reasons.Add(L10n.Format("RecruitWageComparison", offeredWage, person.Wage));
        }
        else reasons.Add(L10n.Get("RecruitWageUnknown"));
        if (person.NationId.HasValue && destination.ClubNationId.HasValue && person.NationId != destination.ClubNationId && Attribute(person, "adaptability") is int adapt && adapt <= 5)
        { score -= 5; reasons.Add(L10n.Get("RecruitAdaptability")); }
        bool differentRole = profile.CategoryKey is null && person.JobKey != profile.Key;
        if (differentRole || person.JobKey is null || person.JobFamilyOnly)
            reasons.Add(L10n.Get("RecruitRoleUnknown"));
        if (person.JobKey == "manager" && profile.Key != "manager")
        { score -= 15; reasons.Add(L10n.Get("RecruitManagerChange")); }
        if (person.EmploymentCount > 1) reasons.Add(L10n.Get("RecruitMultipleJobs"));
        score = Math.Clamp(score, -100, 100);
        StaffInterest band = !repKnown && !expired ? StaffInterest.Uncertain : score >= 10 ? StaffInterest.Likely : score >= -20 ? StaffInterest.Borderline : StaffInterest.Unlikely;
        reasons.Add(L10n.Get("RecruitMissingFactors"));
        return new(band, score, reasons);
    }
    public static IReadOnlyList<StaffCandidate> Rank(StaffRecruitmentResult data, StaffSearchProfile profile, long offeredWage = 0, bool ignoreInterest = false, string search = "")
    {
        if (data.Status != "ready") return [];
        // Avoid treating the easiest of two concurrent employers as the only job to leave.
        return data.Staff.Where(p => !p.IsHumanManager && p.ClubUid != data.ClubUid)
            .GroupBy(p => p.Uid).Select(g => g.OrderByDescending(p => p.ClubReputation ?? int.MaxValue).ThenByDescending(p => p.Wage).First())
            .Where(p => profile.Key != "doctor" || p.JobKey == "doctor")
            .Select(p => new StaffCandidate(p, profile, Quality(p, profile), Estimate(p, profile, data, offeredWage),
                CoachingStars.Calculate(p).Where(c => c.Score.HasValue).OrderByDescending(c => c.Score).FirstOrDefault()))
            .Where(c => (ignoreInterest || c.Interest.Band != StaffInterest.Unlikely) &&
                (string.IsNullOrWhiteSpace(search) || c.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) || c.Club.Contains(search, StringComparison.CurrentCultureIgnoreCase) || c.CurrentRole.Contains(search, StringComparison.CurrentCultureIgnoreCase) || c.Person.Uid.ToString().Contains(search) || c.Person.Id?.ToString().Contains(search) == true))
            .OrderByDescending(c => c.Quality.HasValue).ThenByDescending(c => c.Quality).ThenByDescending(c => ignoreInterest ? 0 : c.Interest.Score).ThenBy(c => c.Name).ThenBy(c => c.Person.Uid).ToArray();
    }
}
