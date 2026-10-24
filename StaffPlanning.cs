using System.Globalization;

namespace FM26SimpleEditor;

public enum StaffRenewalAdvice { Renew, CompareReplacement, VerifyAlternatives, Review }
public sealed record StaffRenewal(StaffRow Person, DateTime End, int Days, StaffSearchProfile? Profile,
    double? Quality, double? Stars, StaffRenewalAdvice Advice, IReadOnlyList<StaffCandidate> Alternatives, bool MarketReady)
{
    public uint Uid => Person.Uid;
    public string Name => Person.Name;
    public string Role => Person.RoleDisplay;
    public string Club => Person.ListedClubName ?? Person.ClubName;
    public string ContractEnd => End.ToString("dd/MM/yyyy", L10n.Culture);
    public long? Wage => Person.Wage;
    public string Criterion => Profile?.Label ?? "—";
    public string Recommendation => L10n.Get("PlanAdvice_" + Advice);
    public StaffCandidate? BestPlausible => Alternatives.FirstOrDefault(c => c.Interest.Band == StaffInterest.Likely);
    public string Replacement => (BestPlausible ?? Alternatives.FirstOrDefault())?.Name ?? "—";
    public int BetterCount => Alternatives.Count;
    public string Explanation => L10n.Get("PlanReason_" + Advice);
}

// Read-only renewal advice: compare the same job/category on identical /20 scales.
public static class StaffPlanning
{
    public const double MeaningfulGain = .75;
    public static DateTime? Date(string? value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var date) ? date : null;
    public static IReadOnlyList<StaffSearchProfile> Profiles(StaffRow person) => StaffRecruitment.CurrentProfiles(person);
    public static StaffSearchProfile? DefaultProfile(StaffRow person) => StaffRecruitment.CurrentProfile(person);
    public static IReadOnlyList<StaffRenewal> Build(StaffResult own, StaffRecruitmentResult? market, long offer = 0,
        IReadOnlyDictionary<uint, string>? choices = null)
    {
        if (own.Status != "ready" || Date(own.GameDate) is not DateTime clock || !own.ClubUid.HasValue) return [];
        bool ready = market?.Status == "ready" && market.ClubUid == own.ClubUid && Date(market.GameDate) == clock;
        var ownIds = own.Staff.Select(p => p.Uid).ToHashSet();
        // Rank once per criterion, not once per expiring employee, over the complete pool.
        var ranks = new Dictionary<string, IReadOnlyList<StaffCandidate>>();
        var result = new List<StaffRenewal>();
        foreach (var person in own.Staff.Where(p => !p.IsHumanManager && p.HasContract)
            .Where(p => Date(p.ContractStart) is not DateTime start || start <= clock)
            .Where(p => Date(p.ContractEnd) is DateTime end && end >= clock && end <= clock.AddMonths(6))
            .DistinctBy(p => (p.Uid, p.ClubUid, p.ContractEnd)))
        {
            var profile = choices is not null && choices.TryGetValue(person.Uid, out string? key)
                ? Profiles(person).SingleOrDefault(p => p.Key == key) ?? DefaultProfile(person) : DefaultProfile(person);
            double? quality = profile is null ? null : StaffRecruitment.Quality(person, profile);
            double? stars = profile is null ? null : new StaffCandidate(person, profile, quality,
                new(StaffInterest.Uncertain, 0, []), null).RoleStars;
            IReadOnlyList<StaffCandidate> alternatives = [];
            if (ready && profile is not null && quality.HasValue)
            {
                if (!ranks.TryGetValue(profile.Key, out var ranked))
                    ranks[profile.Key] = ranked = StaffRecruitment.Rank(market!, profile, offer, currentJob: person.JobKey)
                        .Where(c => !ownIds.Contains(c.Uid) && !c.Person.JobFamilyOnly).ToArray();
                alternatives = ranked.Where(c => c.Quality is double q && q >= quality.Value + MeaningfulGain).ToArray();
            }
            var advice = !ready || !quality.HasValue ? StaffRenewalAdvice.Review :
                alternatives.Any(c => c.Interest.Band == StaffInterest.Likely) ? StaffRenewalAdvice.CompareReplacement :
                alternatives.Count > 0 ? StaffRenewalAdvice.VerifyAlternatives : StaffRenewalAdvice.Renew;
            var expiry = Date(person.ContractEnd)!.Value;
            result.Add(new(person, expiry, (expiry - clock).Days, profile, quality, stars, advice, alternatives, ready));
        }
        return result.OrderBy(p => p.End).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.Uid).ToArray();
    }
}
