using System.Globalization;

namespace FM26SimpleEditor;

public static class Labels
{
    private sealed record AttributeLabel(string En, string It, string Group);
    private static readonly Dictionary<string, AttributeLabel> Attributes = new(StringComparer.Ordinal)
    {
        ["crossing"] = new("Crossing", "Cross", "technical"),
        ["dribbling"] = new("Dribbling", "Dribbling", "technical"),
        ["finishing"] = new("Finishing", "Finalizzazione", "technical"),
        ["heading"] = new("Heading", "Colpi di testa", "technical"),
        ["long_shots"] = new("Long Shots", "Tiri da lontano", "technical"),
        ["marking"] = new("Marking", "Marcatura", "technical"),
        ["off_the_ball"] = new("Off the Ball", "Senza palla", "mental"),
        ["passing"] = new("Passing", "Passaggi", "technical"),
        ["penalty_taking"] = new("Penalty Taking", "Rigori", "technical"),
        ["tackling"] = new("Tackling", "Contrasti", "technical"),
        ["vision"] = new("Vision", "Visione di gioco", "mental"),
        ["handling"] = new("Handling", "Presa", "goalkeeping"),
        ["aerial_reach"] = new("Aerial Reach", "Palle alte", "goalkeeping"),
        ["command_of_area"] = new("Command of Area", "Controllo dell'area", "goalkeeping"),
        ["communication"] = new("Communication", "Comunicazione", "goalkeeping"),
        ["kicking"] = new("Kicking", "Rinvii", "goalkeeping"),
        ["throwing"] = new("Throwing", "Rimesse", "goalkeeping"),
        ["anticipation"] = new("Anticipation", "Anticipazione", "mental"),
        ["decisions"] = new("Decisions", "Decisioni", "mental"),
        ["one_on_ones"] = new("One on Ones", "Uno contro uno", "goalkeeping"),
        ["positioning"] = new("Positioning", "Posizionamento", "mental"),
        ["reflexes"] = new("Reflexes", "Riflessi", "goalkeeping"),
        ["first_touch"] = new("First Touch", "Controllo di palla", "technical"),
        ["technique"] = new("Technique", "Tecnica", "technical"),
        ["flair"] = new("Flair", "Fantasia", "mental"),
        ["corners"] = new("Corners", "Calci d'angolo", "technical"),
        ["teamwork"] = new("Teamwork", "Gioco di squadra", "mental"),
        ["work_rate"] = new("Work Rate", "Impegno", "mental"),
        ["long_throws"] = new("Long Throws", "Lunghe rimesse", "technical"),
        ["eccentricity"] = new("Eccentricity", "Eccentricità", "goalkeeping"),
        ["rushing_out"] = new("Rushing Out", "Uscite", "goalkeeping"),
        ["punching"] = new("Punching", "Respinta di pugno", "goalkeeping"),
        ["acceleration"] = new("Acceleration", "Accelerazione", "physical"),
        ["free_kick_taking"] = new("Free Kick Taking", "Calci di punizione", "technical"),
        ["strength"] = new("Strength", "Forza", "physical"),
        ["stamina"] = new("Stamina", "Resistenza", "physical"),
        ["pace"] = new("Pace", "Velocità", "physical"),
        ["jumping_reach"] = new("Jumping Reach", "Elevazione", "physical"),
        ["leadership"] = new("Leadership", "Carisma", "mental"),
        ["dirtiness"] = new("Dirtiness", "Scorrettezza", "hidden"),
        ["balance"] = new("Balance", "Equilibrio", "physical"),
        ["bravery"] = new("Bravery", "Coraggio", "mental"),
        ["consistency"] = new("Consistency", "Continuità", "hidden"),
        ["aggression"] = new("Aggression", "Aggressività", "mental"),
        ["agility"] = new("Agility", "Agilità", "physical"),
        ["important_matches"] = new("Important Matches", "Partite importanti", "hidden"),
        ["injury_proneness"] = new("Injury Proneness", "Propensione agli infortuni", "hidden"),
        ["versatility"] = new("Versatility", "Versatilità", "hidden"),
        ["natural_fitness"] = new("Natural Fitness", "Integrità fisica", "physical"),
        ["determination"] = new("Determination", "Determinazione", "mental"),
        ["composure"] = new("Composure", "Freddezza", "mental"),
        ["concentration"] = new("Concentration", "Concentrazione", "mental"),
        ["left_foot"] = new("Left Foot", "Piede sinistro", "feet"),
        ["right_foot"] = new("Right Foot", "Piede destro", "feet"),
    };

    private static readonly Dictionary<string, (string En, string It, string Abbreviation)> Positions = new()
    {
        ["GK"] = ("Goalkeeper", "Portiere", "P"),
        ["SW"] = ("Sweeper", "Libero", "L"),
        ["DL"] = ("Defender Left", "Difensore sinistro", "D S"),
        ["DC"] = ("Defender Centre", "Difensore centrale", "D C"),
        ["DR"] = ("Defender Right", "Difensore destro", "D D"),
        ["DM"] = ("Defensive Midfielder", "Mediano", "M"),
        ["ML"] = ("Midfielder Left", "Centrocampista sinistro", "C S"),
        ["MC"] = ("Midfielder Centre", "Centrocampista centrale", "C C"),
        ["MR"] = ("Midfielder Right", "Centrocampista destro", "C D"),
        ["AML"] = ("Attacking Midfielder Left", "Trequartista sinistro", "T S"),
        ["AMC"] = ("Attacking Midfielder Centre", "Trequartista centrale", "T C"),
        ["AMR"] = ("Attacking Midfielder Right", "Trequartista destro", "T D"),
        ["STC"] = ("Striker", "Punta centrale", "A C"),
        ["WBL"] = ("Wing Back Left", "Esterno sinistro", "E S"),
        ["WBR"] = ("Wing Back Right", "Esterno destro", "E D"),
    };

    public static string Attribute(string key, bool italian) => Attributes.TryGetValue(key, out var label) ? italian ? label.It : label.En : key;
    public static string Group(string key) => Attributes.TryGetValue(key, out var label) ? label.Group : "other";
    public static string GroupTitle(string group, bool italian) => (group, italian) switch
    {
        ("technical", true) => "Tecnici", ("technical", false) => "Technical",
        ("mental", true) => "Mentali", ("mental", false) => "Mental",
        ("physical", true) => "Fisici", ("physical", false) => "Physical",
        ("goalkeeping", true) => "Portiere", ("goalkeeping", false) => "Goalkeeping",
        ("hidden", true) => "Nascosti", ("hidden", false) => "Hidden",
        ("feet", true) => "Piedi", ("feet", false) => "Feet",
        _ => italian ? "Altri" : "Other"
    };
    public static string Position(string key, bool italian) => Positions.TryGetValue(key, out var label) ? italian ? label.It : label.En : key;
    public static string PositionCode(string key, bool italian) => Positions.TryGetValue(key, out var label) && italian ? label.Abbreviation : key;
    public static StringComparer Comparer(bool italian) => StringComparer.Create(CultureInfo.GetCultureInfo(italian ? "it-IT" : "en-GB"), true);
    public static IEnumerable<EditableField> Alphabetical(IEnumerable<EditableField> fields, bool italian) => fields.OrderBy(field => Attribute(field.Key, italian), Comparer(italian));
}
