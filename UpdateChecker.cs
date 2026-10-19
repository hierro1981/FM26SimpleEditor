using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FM26SimpleEditor;

public sealed record ReleaseUpdate(Version Version, string Tag, Uri Url);
public static class AppInfo
{
    public const string RepositoryUrl = "https://github.com/hierro1981/FM26SimpleEditor";
    public static Version Version { get; } = new(typeof(AppInfo).Assembly.GetName().Version!.ToString(3));
    public static string Title => "FM26 Simple Editor · v" + Version.ToString(3);
}
public sealed class UpdateChecker
{
    public const string LatestUrl = "https://api.github.com/repos/hierro1981/FM26SimpleEditor/releases/latest";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };
    public static bool TryVersion(string? tag, out Version? version)
    {
        version = null;
        if (tag is null || !Regex.IsMatch(tag, @"\Av?\d+(\.\d+){0,3}\z", RegexOptions.CultureInvariant)) return false;
        string[] parts = tag.TrimStart('v').Split('.');
        while (parts.Length < 3) parts = parts.Append("0").ToArray();
        if (!Version.TryParse(string.Join('.', parts), out var parsed)) return false;
        version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build));
        return parsed.Revision is -1 or 0;
    }
    public static ReleaseUpdate? Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement;
        if (data.TryGetProperty("draft", out var draft) && draft.GetBoolean() ||
            data.TryGetProperty("prerelease", out var pre) && pre.GetBoolean()) return null;
        if (!data.TryGetProperty("tag_name", out var tagValue) || tagValue.ValueKind != JsonValueKind.String ||
            !data.TryGetProperty("html_url", out var urlValue) || urlValue.ValueKind != JsonValueKind.String) return null;
        string? tag = tagValue.GetString();
        if (!TryVersion(tag, out var version) || !Uri.TryCreate(urlValue.GetString(), UriKind.Absolute, out var url) ||
            url.Scheme != "https" || !url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
            !url.AbsolutePath.StartsWith("/hierro1981/FM26SimpleEditor/releases/tag/", StringComparison.Ordinal) ||
            url.UserInfo.Length > 0 || !url.IsDefaultPort) return null;
        return new(version!, tag!, url);
    }
    public async Task<ReleaseUpdate?> LatestAsync(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestUrl);
        request.Headers.UserAgent.ParseAdd("FM26SimpleEditor/" + AppInfo.Version.ToString(3));
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Client.SendAsync(request, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests) throw new HttpRequestException("RateLimit");
        response.EnsureSuccessStatusCode();
        return Parse(await response.Content.ReadAsStringAsync(token));
    }
}
