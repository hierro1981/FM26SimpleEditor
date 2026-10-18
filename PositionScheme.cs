namespace FM26SimpleEditor;

public static class PositionScheme
{
    public static readonly string[] Order = ["GK", "SW", "DL", "DC", "DR", "WBL", "DM", "WBR", "ML", "MC", "MR", "AML", "AMC", "AMR", "STC"];
    public static int Rank(string key) { int rank = Array.IndexOf(Order, key); return rank < 0 ? int.MaxValue : rank; }
    public static string Main(IEnumerable<EditableField> positions) => positions.Where(p => p.Value > 1).OrderByDescending(p => p.Value).ThenBy(p => Rank(p.Key)).FirstOrDefault()?.Key ?? "";

    // Include every accomplished/natural position; fall back to the best known one.
    public static string Compact(IReadOnlyDictionary<string, int> positions, bool italian)
    {
        var keys = positions.Where(p => p.Value >= 15 && Order.Contains(p.Key)).Select(p => p.Key).ToHashSet();
        if (keys.Count == 0)
        {
            var best = positions.Where(p => p.Value > 1 && Order.Contains(p.Key)).OrderByDescending(p => p.Value).ThenBy(p => Rank(p.Key)).FirstOrDefault();
            if (best.Key is null) return "—";
            keys.Add(best.Key);
        }
        var lines = new (string It, string En, string[] Keys)[]
        {
            ("P", "GK", ["GK"]), ("L", "SW", ["SW"]), ("D", "D", ["DL", "DC", "DR"]),
            ("E", "WB", ["WBL", "WBR"]), ("M", "DM", ["DM"]), ("C", "M", ["ML", "MC", "MR"]),
            ("T", "AM", ["AML", "AMC", "AMR"]), ("A", "ST", ["STC"])
        };
        string Side(string key) => key.EndsWith('L') ? italian ? "S" : "L" : key.EndsWith('R') ? italian ? "D" : "R" : "C";
        return string.Join(", ", lines.Where(line => line.Keys.Any(keys.Contains)).Select(line =>
            (italian ? line.It : line.En) + (line.Keys[0] is "GK" or "SW" or "DM" ? "" : " (" + string.Concat(line.Keys.Where(keys.Contains).Select(Side)) + ")")));
    }
}

// Position-based editorial guidance, not FM's tactical-role or CA weighting formula.
public static class RoleImportance
{
    private static readonly Dictionary<string, string[]> Profiles = new()
    {
        ["keeper"] = ["reflexes handling one_on_ones aerial_reach positioning", "agility anticipation concentration communication command_of_area decisions", "passing kicking throwing balance composure rushing_out"],
        ["centralDefender"] = ["marking tackling positioning heading", "anticipation concentration jumping_reach strength decisions", "pace acceleration bravery composure passing teamwork"],
        ["fullback"] = ["tackling positioning pace acceleration", "crossing marking stamina work_rate anticipation", "passing first_touch dribbling decisions teamwork agility"],
        ["wingback"] = ["pace acceleration stamina crossing", "dribbling tackling positioning work_rate", "passing first_touch technique decisions off_the_ball teamwork"],
        ["holding"] = ["positioning tackling anticipation decisions", "passing teamwork concentration strength", "first_touch composure stamina work_rate technique vision"],
        ["midfield"] = ["passing vision decisions first_touch", "teamwork technique stamina work_rate anticipation", "tackling positioning composure off_the_ball long_shots balance"],
        ["wing"] = ["dribbling crossing pace acceleration", "technique off_the_ball first_touch agility", "passing vision decisions stamina work_rate finishing composure"],
        ["creator"] = ["vision passing first_touch technique", "decisions dribbling off_the_ball composure", "finishing long_shots acceleration agility teamwork flair"],
        ["striker"] = ["finishing off_the_ball composure anticipation", "first_touch acceleration pace heading", "technique strength jumping_reach balance decisions passing teamwork"]
    };
    private static string Profile(string position) => position switch { "GK" => "keeper", "SW" or "DC" => "centralDefender", "DL" or "DR" => "fullback", "WBL" or "WBR" => "wingback", "DM" => "holding", "MC" => "midfield", "ML" or "MR" or "AML" or "AMR" => "wing", "AMC" => "creator", "STC" => "striker", _ => "" };
    public static IEnumerable<(string Key, int Tier)> Fields(string position) => Profiles.TryGetValue(Profile(position), out var tiers)
        ? tiers.SelectMany((line, index) => line.Split(' ').Select(key => (key, index + 1))) : [];
    public static int Tier(string position, string attribute)
    {
        if (!Profiles.TryGetValue(Profile(position), out var tiers)) return 0;
        for (int i = 0; i < tiers.Length; i++) if (tiers[i].Split(' ').Contains(attribute)) return i + 1;
        return 0;
    }
    public static Color Ink(int tier) => tier switch { 1 => Color.FromArgb(115, 42, 146), 2 => Color.FromArgb(25, 83, 172), 3 => Color.FromArgb(0, 111, 105), _ => SystemColors.ControlText };
    public static Color Back(int tier) => tier switch { 1 => Color.FromArgb(245, 230, 250), 2 => Color.FromArgb(231, 240, 255), 3 => Color.FromArgb(229, 246, 239), _ => SystemColors.Window };
    public static string Name(int tier, bool italian) => (tier, italian) switch { (1,true) => "Fondamentale", (2,true) => "Importante", (3,true) => "Utile", (1,false) => "Essential", (2,false) => "Important", (3,false) => "Useful", _ => "" };
}

