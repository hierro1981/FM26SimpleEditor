using System.Text.RegularExpressions;

namespace FM26SimpleEditor;

// Desired vacancy and readable current assignment are separate facts.
public static class StaffSquads
{
    public static IReadOnlyList<string> Targets { get; } = ["any", "first", "subteam", "reserves", "youth", "u23", "u21", "u20", "u19", "u18"];
    public static IReadOnlyList<string> Filters { get; } = Targets.Concat(["unknown"]).ToArray();
    public static string Label(string key) => L10n.Get("StaffSquad_" + key);
    public static bool Youth(string key) => key == "youth" || Regex.IsMatch(key, "^u(18|19|20|21|23)$");
    public static string? AgeGroup(StaffRow person)
    {
        if (!person.HasContract || string.IsNullOrWhiteSpace(person.TeamName)) return null;
        var match = Regex.Match(person.TeamName, @"(?:^|\W)(?:u|under)[\s-]*(18|19|20|21|23)(?:\W|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? "u" + match.Groups[1].Value : null;
    }
    private static bool Affiliate(StaffRow person) => person.TeamClubUid.HasValue && person.TeamClubUid != person.ClubUid;
    public static string TargetFor(StaffRow person) => !person.HasContract || !person.TeamId.HasValue ? "any" :
        AgeGroup(person) ?? (Affiliate(person) ? "subteam" : person.TeamSlot == 0 ? "first" : person.TeamSlot is > 0 ? "subteam" : "any");
    public static bool Matches(StaffRow person, string filter) => filter switch
    {
        "any" => true,
        "unknown" => !person.HasContract || !person.TeamId.HasValue || person.TeamSlot is null && string.IsNullOrWhiteSpace(person.TeamName),
        "first" => person.HasContract && person.TeamId.HasValue && person.TeamSlot == 0 && AgeGroup(person) is null && !Affiliate(person),
        "subteam" => person.HasContract && person.TeamId.HasValue && (person.TeamSlot is > 0 || AgeGroup(person) is not null || Affiliate(person)),
        "youth" => AgeGroup(person) is not null,
        "reserves" => person.HasContract && person.TeamId.HasValue && Regex.IsMatch(person.TeamName ?? (Affiliate(person) ? person.TeamClubName : null) ?? "", @"(?:^|\W)(?:B|C|II|reserves|riserve)(?:\W|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        _ => Youth(filter) && AgeGroup(person) == filter
    };
    public static bool SubteamTarget(string target) => Targets.Contains(target) && target is not ("any" or "first");
    // Assignment evidence is a shortlist preference, never a bonus to attributes or willingness.
    public static int AssignmentPriority(StaffRow person, string target)
    {
        if (!SubteamTarget(target)) return 0;
        if ((Youth(target) && Matches(person, "youth")) || Matches(person, target)) return 0;
        if (Matches(person, "subteam")) return 1;
        return Matches(person, "first") ? 3 : 2;
    }
    public static bool UsesYouthWork(StaffSearchProfile profile) => profile.CategoryKey is not null || profile.Key is
        "manager" or "assistant_manager" or "coach" or "goalkeeping_coach" or "fitness_coach" or "set_piece_coach" or "head_youth_development" or "head_player_development";
    public static StaffInterestEstimate ForTarget(StaffInterestEstimate estimate, StaffRow person, string target)
    {
        if (target is "any" or "first") return estimate;
        var reasons = estimate.Reasons.Concat([L10n.Format("RecruitSquadInterestNote", Label(target))]).ToList();
        var band = estimate.Band;
        if (Matches(person, "first"))
        {
            reasons.Add(L10n.Get("RecruitFirstToSubteam"));
            if (band == StaffInterest.Likely) band = StaffInterest.Borderline;
        }
        return estimate with { Band = band, Reasons = reasons };
    }
}
