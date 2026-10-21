using System.Text.Json;

namespace FM26SimpleEditor;

public sealed record SquadTarget(string Position, string? RoleId = null)
{
    public string Key => RoleId is null ? Position : Position + "|" + RoleId;
}

public sealed class TacticalRoleDefinition
{
    public string Id { get; set; } = "";
    public string Phase { get; set; } = "";
    public string English { get; set; } = "";
    public string Italian { get; set; } = "";
    public List<string> Positions { get; set; } = [];
    public List<string> Focus { get; set; } = [];
    public List<string> Support { get; set; } = [];
    public string Label => L10n.Get(Phase == "ip" ? "TacticalPhaseIP" : "TacticalPhaseOP") + " · " + (L10n.IsItalian ? Italian : English);
    public override string ToString() => Label;
}

public static class TacticalRoles
{
    public const string OfficialSource = "https://www.footballmanager.com/fm26/features/possession-out-possession-fm26s-new-tactical-evolution";
    public const string CatalogueSource = "https://fm.guidetofootball.com/fm26/roles/";
    private static readonly Lazy<IReadOnlyList<TacticalRoleDefinition>> Definitions = new(Read);
    public static IReadOnlyList<TacticalRoleDefinition> All => Definitions.Value;
    private static IReadOnlyList<TacticalRoleDefinition> Read()
    {
        using var stream = typeof(TacticalRoles).Assembly.GetManifestResourceStream("FM26SimpleEditor.TacticalRoles.json")
            ?? throw new InvalidOperationException("Missing FM26 role catalogue");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("roles").Deserialize<List<TacticalRoleDefinition>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    public static IEnumerable<TacticalRoleDefinition> ForPosition(string position) => All.Where(r => r.Positions.Contains(position));
    public static bool CanUse(string? role, string position) => role is null || ForPosition(position).Any(r => r.Id == role);
    public static string Label(SquadTarget target) => Labels.Position(target.Position, L10n.IsItalian) +
        (target.RoleId is null ? "" : " · " + All.Single(r => r.Id == target.RoleId).Label);
    public static string LabelKey(string key)
    {
        var parts = key.Split('|', 2);
        return Label(new(parts[0], parts.Length == 1 ? null : parts[1]));
    }

    // No published official numerical formula was located. These are editor weights.
    // General position: existing 3/2/1 profile. Named role: focus 3, support 2,
    // remaining general-position fields 1. Missing/duplicate fields invalidate a score.
    public static IReadOnlyDictionary<string, int> Weights(SquadTarget target)
    {
        if (!PositionScheme.Order.Contains(target.Position) || !CanUse(target.RoleId, target.Position)) throw new ArgumentException(nameof(target));
        var fields = RoleImportance.Fields(target.Position).ToDictionary(f => f.Key, f => target.RoleId is null ? 4 - f.Tier : 1);
        if (target.RoleId is not null)
        {
            var role = All.Single(r => r.Id == target.RoleId);
            foreach (string key in role.Support) fields[key] = 2;
            foreach (string key in role.Focus) fields[key] = 3;
        }
        return fields;
    }
    public static double? Score(PlayerDetail player, SquadTarget target)
    {
        var weights = Weights(target);
        var values = player.Attributes.GroupBy(a => a.Key).Where(g => g.Count() == 1 && g.First().Value is >= 1 and <= 125)
            .ToDictionary(g => g.Key, g => g.First().Value);
        if (weights.Count == 0 || weights.Keys.Any(key => !values.ContainsKey(key))) return null;
        return weights.Sum(f => PreviewMath.Attribute20(values[f.Key]) * f.Value) / (double)weights.Values.Sum();
    }
}
