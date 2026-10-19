using System.Globalization;
namespace FM26SimpleEditor;
public static class Labels
{
    private static readonly Dictionary<string, string> Groups = new(StringComparer.Ordinal)
    {
        ["crossing"] = "technical",
        ["dribbling"] = "technical",
        ["finishing"] = "technical",
        ["heading"] = "technical",
        ["long_shots"] = "technical",
        ["marking"] = "technical",
        ["off_the_ball"] = "mental",
        ["passing"] = "technical",
        ["penalty_taking"] = "technical",
        ["tackling"] = "technical",
        ["vision"] = "mental",
        ["handling"] = "goalkeeping",
        ["aerial_reach"] = "goalkeeping",
        ["command_of_area"] = "goalkeeping",
        ["communication"] = "goalkeeping",
        ["kicking"] = "goalkeeping",
        ["throwing"] = "goalkeeping",
        ["anticipation"] = "mental",
        ["decisions"] = "mental",
        ["one_on_ones"] = "goalkeeping",
        ["positioning"] = "mental",
        ["reflexes"] = "goalkeeping",
        ["first_touch"] = "technical",
        ["technique"] = "technical",
        ["flair"] = "mental",
        ["corners"] = "technical",
        ["teamwork"] = "mental",
        ["work_rate"] = "mental",
        ["long_throws"] = "technical",
        ["eccentricity"] = "goalkeeping",
        ["rushing_out"] = "goalkeeping",
        ["punching"] = "goalkeeping",
        ["acceleration"] = "physical",
        ["free_kick_taking"] = "technical",
        ["strength"] = "physical",
        ["stamina"] = "physical",
        ["pace"] = "physical",
        ["jumping_reach"] = "physical",
        ["leadership"] = "mental",
        ["dirtiness"] = "hidden",
        ["balance"] = "physical",
        ["bravery"] = "mental",
        ["consistency"] = "hidden",
        ["aggression"] = "mental",
        ["agility"] = "physical",
        ["important_matches"] = "hidden",
        ["injury_proneness"] = "hidden",
        ["versatility"] = "hidden",
        ["natural_fitness"] = "physical",
        ["determination"] = "mental",
        ["composure"] = "mental",
        ["concentration"] = "mental",
        ["left_foot"] = "feet",
        ["right_foot"] = "feet",
    };
    private static readonly HashSet<string> Positions = new(StringComparer.Ordinal) { "GK", "SW", "DL", "DC", "DR", "DM", "ML", "MC", "MR", "AML", "AMC", "AMR", "STC", "WBL", "WBR" };
    public static string Attribute(string key, bool italian) => Groups.ContainsKey(key) ? L10n.Get("Attribute_" + key, italian) : key;
    public static string Group(string key) => Groups.GetValueOrDefault(key, "other");
    public static string GroupTitle(string group, bool italian) => L10n.Get("Group_" + (new[] { "technical", "mental", "physical", "goalkeeping", "hidden", "feet" }.Contains(group) ? group : "other"), italian);
    public static string Position(string key, bool italian) => Positions.Contains(key) ? L10n.Get("Position_" + key, italian) : key;
    public static string PositionCode(string key, bool italian) => Positions.Contains(key) ? L10n.Get("PositionCode_" + key, italian) : key;
    public static StringComparer Comparer(bool italian) => StringComparer.Create(CultureInfo.GetCultureInfo(italian ? "it-IT" : "en-GB"), true);
    public static IEnumerable<EditableField> Alphabetical(IEnumerable<EditableField> fields, bool italian) => fields.OrderBy(field => Attribute(field.Key, italian), Comparer(italian));
}
