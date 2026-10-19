using System.Text.Json;

namespace FM26SimpleEditor;

public sealed record AppSettings(string Language = "it")
{
    public const string FileName = "FM26SimpleEditor.config.json";
    public static AppSettings Load(string directory)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, FileName)));
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("language", out var value) && value.ValueKind == JsonValueKind.String && value.GetString() == "en")
                return new("en");
        }
        catch (Exception exc) when (exc is IOException or UnauthorizedAccessException or JsonException) { }
        return new("it");
    }
    public static void Save(string directory, string language)
    {
        if (language is not ("it" or "en")) throw new ArgumentException("Unsupported language", nameof(language));
        Directory.CreateDirectory(directory);
        string destination = Path.Combine(directory, FileName);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { language }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
