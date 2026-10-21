using System.Text.Json;

namespace FM26SimpleEditor;

public sealed record AppSettings(string Language = "it", int RecommendationCount = 10)
{
    public const string FileName = "FM26SimpleEditor.config.json";
    public static AppSettings Load(string directory)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, FileName)));
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = document.RootElement;
                string language = root.TryGetProperty("language", out var value) && value.ValueKind == JsonValueKind.String && value.GetString() == "en" ? "en" : "it";
                int count = root.TryGetProperty("recommendationCount", out var number) && number.ValueKind == JsonValueKind.Number && number.TryGetInt32(out int n) && n is >= 1 and <= 100 ? n : 10;
                return new(language, count);
            }
        }
        catch (Exception exc) when (exc is IOException or UnauthorizedAccessException or JsonException) { }
        return new("it");
    }
    public static void Save(string directory, string language) => Save(directory, Load(directory) with { Language = language });
    public static void Save(string directory, AppSettings settings)
    {
        string language = settings.Language;
        if (settings.RecommendationCount is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(settings));
        if (language is not ("it" or "en")) throw new ArgumentException("Unsupported language", nameof(language));
        Directory.CreateDirectory(directory);
        string destination = Path.Combine(directory, FileName);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { language, recommendationCount = settings.RecommendationCount }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
