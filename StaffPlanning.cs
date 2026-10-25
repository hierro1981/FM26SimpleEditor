using System.Globalization;

namespace FM26SimpleEditor;

public enum StaffContractWindow { Expired, SixMonths, TwelveMonths, All }
public enum StaffRenewalAdvice { Renew, CompareReplacement, VerifyAlternatives, Review }
public sealed record StaffRenewal(StaffRow Person, DateTime End, int Days, StaffSearchProfile? Profile,
    double? Quality, double? Stars, StaffRenewalAdvice Advice, IReadOnlyList<StaffCandidate> Alternatives, bool MarketReady)
{
    public uint Uid => Person.Uid;
    public string Name => Person.Name;
    public string Role => Person.RoleDisplay;
    public string Team => Person.TeamDisplay;
    public string ContractEnd => End.ToString("dd/MM/yyyy", L10n.Culture);
    public long? Wage => Person.Wage;
    public string Criterion => Profile?.Key == "analyst" && Person.JobFamilyOnly ? Person.RoleDisplay : Profile?.Label ?? "—";
    public string Recommendation => L10n.Get("PlanAdvice_" + Advice);
    public StaffCandidate? BestPlausible => Alternatives.FirstOrDefault(c => c.Interest.Band == StaffInterest.Likely);
    public StaffCandidate? Proposed => BestPlausible ?? Alternatives.FirstOrDefault();
    public string Replacement => Proposed?.Name ?? "—";
    public double? ReplacementStars => Proposed?.RoleStars;
    public string ReplacementTeam => Proposed?.Team ?? "—";
    public int BetterCount => Alternatives.Count;
    public string Explanation
    {
        get
        {
            string text = L10n.Get("PlanReason_" + Advice);
            var missing = Profile?.Weights.Keys.Where(k => StaffRecruitment.Attribute(Person,k) is null).ToArray() ?? [];
            if (Advice == StaffRenewalAdvice.Review && missing.Length > 0)
                text = L10n.Format("PlanMissingAttributes", string.Join(", ", missing.Select(k=>L10n.Get("StaffAttribute_"+k))));
            return text + (StaffRecruitment.AnalystJob(Person).HasValue ? " " + L10n.Get("AnalystCategoryNote") : "");
        }
    }
}

// Read-only renewal advice: compare the same job/category on identical /20 scales.
public static class StaffPlanning
{
    public const double MeaningfulGain = .75;
    public static DateTime? Date(string? value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var date) ? date : null;
    public static bool InWindow(DateTime end, DateTime clock, StaffContractWindow window) => window switch
    {
        StaffContractWindow.Expired => end < clock.AddDays(3),
        StaffContractWindow.SixMonths => end >= clock && end < clock.AddMonths(6),
        StaffContractWindow.TwelveMonths => end >= clock && end < clock.AddMonths(12),
        StaffContractWindow.All => true,
        _ => false
    };
    public static IReadOnlyList<StaffSearchProfile> Profiles(StaffRow person) => StaffRecruitment.CurrentProfiles(person);
    public static StaffSearchProfile? DefaultProfile(StaffRow person) => StaffRecruitment.CurrentProfile(person);
    public static IReadOnlyList<StaffRenewal> Build(StaffResult own, StaffRecruitmentResult? market, long offer = 0,
        IReadOnlyDictionary<uint, string>? choices = null, StaffContractWindow window = StaffContractWindow.SixMonths)
    {
        if (own.Status != "ready" || Date(own.GameDate) is not DateTime clock || !own.ClubUid.HasValue) return [];
        bool ready = market?.Status == "ready" && market.ClubUid == own.ClubUid && Date(market.GameDate) == clock;
        var ownIds = own.Staff.Select(p => p.Uid).ToHashSet();
        // Rank once per criterion, not once per expiring employee, over the complete pool.
        var ranks = new Dictionary<string, IReadOnlyList<StaffCandidate>>();
        var result = new List<StaffRenewal>();
        foreach (var person in own.Staff.Where(p => !p.IsHumanManager && p.HasContract)
            .Where(p => Date(p.ContractStart) is not DateTime start || start <= clock)
            .Where(p => Date(p.ContractEnd) is DateTime end && InWindow(end, clock, window))
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
                string target = StaffSquads.TargetFor(person);
                int? analystCode = StaffRecruitment.AnalystJob(person);
                string rankKey = profile.Key + "|" + target + "|" + analystCode;
                if (!ranks.TryGetValue(rankKey, out var ranked))
                    ranks[rankKey] = ranked = StaffRecruitment.Rank(market!, profile, offer, currentJob: person.JobKey, targetSquad: target, currentJobCode: analystCode)
                        .Where(c => !ownIds.Contains(c.Uid) && StaffRecruitment.CanAssessCurrent(c.Person)).ToArray();
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
