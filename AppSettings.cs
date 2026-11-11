using System.Text.Json;

namespace FM26SimpleEditor;

public sealed record AppSettings(string Language = "it", int RecommendationCount = 10, bool ShowNotes = true)
{
    public const string FileName = "FM26SimpleEditor.config.json";
    public static bool HasLanguage(string directory)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, FileName)));
            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("language", out var value) && value.ValueKind == JsonValueKind.String &&
                value.GetString() is "it" or "en";
        }
        catch (Exception exc) when (exc is IOException or UnauthorizedAccessException or JsonException) { return false; }
    }
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
                bool notes = !root.TryGetProperty("showNotes", out var nvalue) || nvalue.ValueKind != JsonValueKind.False;
                return new(language, count, notes);
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
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { language, recommendationCount = settings.RecommendationCount, showNotes = settings.ShowNotes }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public static class UiNotes { public static bool Show = true; }
public static class TextSearch
{
    public static bool Contains(string? text, string? query) => System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(text ?? "", query ?? "", System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;
}
