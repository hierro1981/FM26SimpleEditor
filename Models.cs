using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FM26SimpleEditor;

public sealed class PlayerRow
{
    public uint Uid { get; set; }
    public uint? Id { get; set; }
    public string Name { get; set; } = "";
    public string Team { get; set; } = "";
    public uint? OwnerClubUid { get; set; }
    public string OwnerTeam { get; set; } = "";
    [JsonIgnore] public bool ShowLoanOwner { get; set; }
    [JsonIgnore] public string SearchTeam => ShowLoanOwner && OnLoan is true ? OwnerTeam : Team;
    [JsonIgnore] public string SearchTeamDisplay => SearchTeam.Length > 0 ? SearchTeam : ShowLoanOwner && OnLoan is true ? L10n.Get("Text_7fa7c40c16c5") : "—";
    [JsonIgnore] public string LoanDescription => OnLoan is true ? L10n.Format("Text_207544cd934d", (Team.Length > 0 ? Team : L10n.Get("Text_1d80f0682e0b")), (OwnerTeam.Length > 0 ? OwnerTeam : L10n.Get("Text_1d80f0682e0b"))) : OnLoan is null ? L10n.Get("Text_b959d6541181") : "";
    public string MainPosition { get; set; } = "";
    public string? Birth { get; set; }
    public int Ca { get; set; }
    public int? Pa { get; set; }
    public int? PaRange { get; set; }
    public uint? TeamId { get; set; }
    public uint? ClubUid { get; set; }
    public int? Age { get; set; }
    public int? Height { get; set; }
    public int LeftFoot { get; set; }
    public int RightFoot { get; set; }
    public bool? OnLoan { get; set; }
    public Dictionary<string, int> Positions { get; set; } = new();
    public string? ContractEnd { get; set; }
    public string ContractStatus { get; set; } = "unknown";
    public string PositionSummary => string.Join(", ", Positions.Where(pair => pair.Value >= 12).OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} ({pair.Value})"));
    public string ContractLabel => ContractStatus switch { "none" => L10n.Get("Text_f53c05dd871c"), "expired" => L10n.Get("Text_89bf53640b26"), "active" => L10n.Get("Text_52fcfde046ff"), _ => L10n.Get("Text_ce1391381fc8") };
    public string ContractEndDisplay => DateTime.TryParseExact(ContractEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.ToString("dd/MM/yyyy") : "—";
}

public sealed class EditableField : INotifyPropertyChanged
{
    public int Index { get; set; }
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    private int value;
    public int Value
    {
        get => value;
        set { if (this.value == value) return; this.value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class PlayerDetail
{
    public PlayerRow Player { get; set; } = new();
    public List<EditableField> Attributes { get; set; } = [];
    public List<EditableField> Positions { get; set; } = [];
    public bool BirthEditable { get; set; }
    public string BirthWarning { get; set; } = "";
    public LeagueReference League { get; set; } = new();
    public int RawPa { get; set; }
}

public sealed class LeagueReference
{
    public double? Mean { get; set; }
    public int Count { get; set; }
    public string Label { get; set; } = L10n.Get("Text_3cad6e3f2783");
}

public sealed class OpenResult
{
    public string Source { get; set; } = "";
    public string? GameDate { get; set; }
    public List<PlayerRow> Players { get; set; } = [];
}

public sealed class SaveResult
{
    public string Source { get; set; } = "";
    public PlayerDetail Detail { get; set; } = new();
}

public sealed class SearchResult
{
    public int Total { get; set; }
    public List<uint> Uids { get; set; } = [];
}

public sealed class SearchRequest
{
    public string Command { get; set; } = "search";
    public string Text { get; set; } = "";
    public string? TeamExact { get; set; }
    public string? TeamContains { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    public int? CaMin { get; set; }
    public int? CaMax { get; set; }
    public int? PaMin { get; set; }
    public int? PaMax { get; set; }
    public int? HeightMin { get; set; }
    public int? HeightMax { get; set; }
    public string? Position { get; set; }
    public int PositionMin { get; set; } = 15;
    public int? GrowthMin { get; set; }
    public string Contract { get; set; } = "all";
    public string Foot { get; set; } = "all";
    public string Loan { get; set; } = "all";
    public string LoanTeam { get; set; } = "playing";
    public string Sort { get; set; } = "name";
    public bool Descending { get; set; }
}

public sealed class HistoryPoint
{
    public int Sequence { get; set; }
    public string Date { get; set; } = "";
    public string Source { get; set; } = "";
    public string Name { get; set; } = "";
    public int? Ca { get; set; }
    public int? Pa { get; set; }
    public Dictionary<string, int> Attributes { get; set; } = new();
    public string IdentityNote { get; set; } = "";
}

public sealed class HistoryResult
{
    public List<HistoryPoint> EmbeddedPoints { get; set; } = [];
    public string EmbeddedMessage { get; set; } = "";
    public List<HistoryPoint> Points { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public bool EmbeddedAvailable { get; set; }
    public string Message { get; set; } = "";
}

public static class PreviewMath
{
    public static string DeltaText(int delta, int qualityDelta) => qualityDelta > 0 ? $"← {delta:+0;-0;0}" : qualityDelta < 0 ? $"{delta:+0;-0;0} →" : "0 =";
    public static double? AbsoluteStars(int? ability) => ability is null or <= 0 ? null : Math.Clamp(Math.Round(ability.Value / 40.0 * 2, MidpointRounding.AwayFromZero) / 2, 0.5, 5);

    public static int Attribute20(int raw) => Math.Clamp((raw + 2) / 5, 1, 20);

    public static double? Stars(int? ability, double? squadMean)
    {
        if (ability is null or <= 0 || squadMean is null) return null;
        var estimate = 2.5 + (ability.Value - squadMean.Value) / 20.0;
        return Math.Clamp(Math.Round(estimate * 2, MidpointRounding.AwayFromZero) / 2.0, 0.5, 5.0);
    }

    public static int[] StoredOrder(IEnumerable<EditableField> fields)
    {
        var list = fields.OrderBy(field => field.Index).ToArray();
        if (!list.Select(field => field.Index).SequenceEqual(Enumerable.Range(0, list.Length)))
            throw new InvalidOperationException(L10n.Get("Text_398c94104b68"));
        return list.Select(field => field.Value).ToArray();
    }
}

public sealed class BackendProgress
{
    public int Percent { get; set; }
    public string Phase { get; set; } = "";
    public long? Done { get; set; }
    public long? Total { get; set; }
    public string Unit { get; set; } = "";
}




public sealed class ManagedSquad
{
    public uint ClubUid { get; set; }
    public string ClubName { get; set; } = "";
    public string? ManagerName { get; set; }
    public uint FirstTeamId { get; set; }
    public List<uint> TeamIds { get; set; } = [];
    public List<ManagedSquadTeam> Teams { get; set; } = [];
    public override string ToString() => ClubName + (string.IsNullOrEmpty(ManagerName) ? "" : " · " + ManagerName);
}

public sealed class MySquadResult
{
    public string Status { get; set; } = "none";
    public List<ManagedSquad> Clubs { get; set; } = [];
    public List<PlayerDetail> Players { get; set; } = [];
    public bool FirstHumanOnly { get; set; } = true;
    public string? Error { get; set; }
}

public sealed class StaffRow
{
    [JsonIgnore] public double? CurrentRoleStars => StaffRecruitment.CurrentStars(this);
    public uint Uid { get; set; }
    public uint? Id { get; set; }
    public string Name { get; set; } = "";
    public uint ClubUid { get; set; }
    public string ClubName { get; set; } = "";
    public uint? ListedClubUid { get; set; }
    public string? ListedClubName { get; set; }
    public uint? TeamId { get; set; }
    public int? TeamSlot { get; set; }
    public int? TeamTypeCode { get; set; }
    public int? TeamAgeLimit { get; set; }
    public string? TeamTypeMethod { get; set; }
    public string? TeamName { get; set; }
    public uint? TeamClubUid { get; set; }
    public string? TeamClubName { get; set; }
    [JsonIgnore] public string TeamDisplay
    {
        get
        {
            if (!HasContract || !TeamId.HasValue) return L10n.Get("StaffTeamUnknown");
            string label = TeamTypeMethod == "unclassified_subteam" ? L10n.Get("StaffAdditionalSquad") : !string.IsNullOrWhiteSpace(TeamName) ? TeamName : TeamSlot == 0 ? L10n.Get("StaffFirstTeam") :
                TeamSlot is > 0 ? L10n.Format("StaffSubteamSlot", TeamSlot) : L10n.Get("StaffTeamUnknown");
            if (TeamSlot is null && string.IsNullOrWhiteSpace(TeamName)) label += " · ID " + TeamId;
            return TeamClubUid.HasValue && TeamClubUid != ClubUid && !string.IsNullOrWhiteSpace(TeamClubName) ? TeamClubName + " · " + label : label;
        }
    }
    [JsonIgnore] public string TeamTooltip => L10n.Format("StaffTeamContext", TeamDisplay, TeamId?.ToString(L10n.Culture) ?? "—") + "\n" + L10n.Get("StaffTeamNote") + (TeamTypeCode.HasValue ? "\n" + L10n.Format("StaffTypeEvidence", TeamTypeCode, TeamAgeLimit == 255 ? "—" : TeamAgeLimit?.ToString(L10n.Culture) ?? "—") : "");
    public int? NationId { get; set; }
    public int? ClubNationId { get; set; }
    public int? ClubReputation { get; set; }
    public int EmploymentCount { get; set; }
    public int? Age { get; set; }
    public string? Birth { get; set; }
    public bool IsHumanManager { get; set; }
    public bool HasContract { get; set; }
    public int? JobCode { get; set; }
    public string? JobKey { get; set; }
    public bool JobFamilyOnly { get; set; }
    public bool IsPlayerJob { get; set; }
    public long? Wage { get; set; }
    public string? ContractStart { get; set; }
    public string? ContractEnd { get; set; }
    public int? Ca { get; set; }
    public int? Pa { get; set; }
    public Dictionary<string, int> Reputation { get; set; } = new();
    public Dictionary<string, int> JobPreferences { get; set; } = new();
    [JsonIgnore] public int? CurrentReputation => Reputation.TryGetValue("current", out int value) && value is >= 1 and <= 10000 ? value : null;
    public Dictionary<string, int> Personality { get; set; } = new();
    public Dictionary<string, int> Attributes { get; set; } = new();
    public Dictionary<string, int> Preferences { get; set; } = new();
    [JsonIgnore] public string RoleDisplay
    {
        get
        {
            if (IsHumanManager) return L10n.Get("StaffHuman");
            string key = "StaffJob_" + JobKey;
            if (!JobCode.HasValue || string.IsNullOrEmpty(JobKey) || !L10n.Keys.Contains(key)) return L10n.Get("StaffUnknownRole");
            string label = L10n.Get(key);
            if (JobFamilyOnly) label = L10n.Format("StaffJobFamilyFormat", label);
            return IsPlayerJob ? L10n.Format("StaffPlayerJobFormat", label) : label;
        }
    }
    [JsonIgnore] public string RoleTooltip => JobFamilyOnly ? L10n.Get("StaffJobFamilyNote") : "";
    [JsonIgnore] public string ContractDisplay => L10n.Get(HasContract ? "StaffContractFound" : "StaffContractMissing");
    [JsonIgnore] public string BirthDisplay => DisplayDate(Birth);
    [JsonIgnore] public string ContractStartDisplay => DisplayDate(ContractStart);
    [JsonIgnore] public string ContractEndDisplay => DisplayDate(ContractEnd);
    private static string DisplayDate(string? value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.ToString("dd/MM/yyyy") : "—";
}

public sealed class StaffResult
{
    public string? GameDate { get; set; }
    public string Status { get; set; } = "none";
    public uint? ClubUid { get; set; }
    public List<StaffRow> Staff { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class StaffRecruitmentResult
{
    public string Status { get; set; } = "none";
    public uint? ClubUid { get; set; }
    public string ClubName { get; set; } = "";
    public int? ClubReputation { get; set; }
    public int? ClubNationId { get; set; }
    public string? GameDate { get; set; }
    public int PoolPeople { get; set; }
    public int OwnExcluded { get; set; }
    public List<string> ReaderWarnings { get; set; } = [];
    public List<StaffRow> Staff { get; set; } = [];
    public string? Error { get; set; }
}

public sealed class ManagedSquadTeam
{
    public uint TeamId { get; set; }
    public int? Slot { get; set; }
    public string? Name { get; set; }
    public string? TypeMethod { get; set; }
}
