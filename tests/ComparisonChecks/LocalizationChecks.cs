using System.Collections;
using System.Net.Http;
using System.Resources;
using System.Text.Json;
using System.Text.RegularExpressions;
using FM26SimpleEditor;

internal static class LocalizationChecks
{
    private static IEnumerable<Control> All(Control c) => c.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(All(x)));
    public static void Run(Action<bool, string> check)
    {
        var resource = new ResourceManager("FM26SimpleEditor.Resources.Strings", typeof(L10n).Assembly);
        var english = resource.GetResourceSet(System.Globalization.CultureInfo.GetCultureInfo("en"), true, false)!;
        check(english is not null && english.Cast<DictionaryEntry>().Count() == L10n.Keys.Count, "English satellite contains every resource key");
        foreach (string key in L10n.Keys)
        {
            string it = L10n.Get(key, true), en = L10n.Get(key, false);
            var a = Regex.Matches(it, @"\{(\d+)(?:[^}]*)\}").Select(m => m.Groups[1].Value).Order().ToArray();
            var b = Regex.Matches(en, @"\{(\d+)(?:[^}]*)\}").Select(m => m.Groups[1].Value).Order().ToArray();
            if (string.IsNullOrEmpty(it) || string.IsNullOrEmpty(en) || !a.SequenceEqual(b)) throw new InvalidOperationException("Resource parity: " + key);
        }
        check(true, "all resource translations preserve format placeholders");
        string directory = Path.Combine(Path.GetTempPath(), "FM26-languages-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            check(AppSettings.Load(directory).Language == "it", "missing configuration defaults to Italian");
            AppSettings.Save(directory, "en");
            check(AppSettings.Load(directory).Language == "en" && JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, AppSettings.FileName))).RootElement.GetProperty("language").GetString() == "en", "language preference round-trips through local JSON");
            File.WriteAllText(Path.Combine(directory, AppSettings.FileName), "{broken");
            check(AppSettings.Load(directory).Language == "it", "malformed configuration does not block startup");
            File.WriteAllText(Path.Combine(directory, AppSettings.FileName), "{\"language\":42}");
            check(AppSettings.Load(directory).Language == "it", "invalid configuration field type falls back safely");
            File.WriteAllText(Path.Combine(directory, AppSettings.FileName), "{\"language\":\"fr\"}");
            check(AppSettings.Load(directory).Language == "it", "unsupported stored language falls back safely");
            AppSettings.Save(directory, "it");
            check(Directory.GetFiles(directory, "*.tmp").Length == 0, "atomic configuration save leaves no temporary file");
            bool denied = false;
            File.SetAttributes(Path.Combine(directory, AppSettings.FileName), FileAttributes.ReadOnly);
            try { AppSettings.Save(directory, "en"); } catch (Exception exc) when (exc is IOException or UnauthorizedAccessException) { denied = true; }
            check(denied && AppSettings.Load(directory).Language == "it" && Directory.GetFiles(directory, "*.tmp").Length == 0, "unwritable configuration preserves previous preference");
            File.SetAttributes(Path.Combine(directory, AppSettings.FileName), FileAttributes.Normal);
            Ui(check, directory);
        }
        finally { L10n.SetLanguage("it"); Directory.Delete(directory, recursive: true); }
        check(UpdateChecker.TryVersion("v13", out var simple) && UpdateChecker.TryVersion("v13.0.0", out var full) && simple == full, "release version components normalize");
        check(UpdateChecker.TryVersion("v9", out var older) && UpdateChecker.TryVersion("v13.2", out var newer) && older < AppInfo.Version && newer > AppInfo.Version, "release version comparison is numeric");
        check(!UpdateChecker.TryVersion("v14-beta", out _) && !UpdateChecker.TryVersion("latest", out _) && !UpdateChecker.TryVersion("v999999999999", out _), "invalid and prerelease versions are rejected");
        string Json(string tag, string url, bool draft = false, bool pre = false) => JsonSerializer.Serialize(new { tag_name = tag, html_url = url, draft, prerelease = pre });
        check(UpdateChecker.Parse(Json("v14", AppInfo.RepositoryUrl + "/releases/tag/v14"))?.Version > AppInfo.Version, "valid newer GitHub release is recognized");
        check(UpdateChecker.Parse(Json("v14", "https://example.org/releases/tag/v14")) is null && UpdateChecker.Parse(Json("v14", "http://github.com/hierro1981/FM26SimpleEditor/releases/tag/v14")) is null, "release links are restricted to this HTTPS GitHub repository");
        check(UpdateChecker.Parse(Json("v14", AppInfo.RepositoryUrl + "/releases/tag/v14", pre: true)) is null && UpdateChecker.Parse(Json("v14", AppInfo.RepositoryUrl + "/releases/tag/v14", draft: true)) is null && UpdateChecker.Parse("{}") is null, "draft, prerelease and incomplete release payloads are ignored");
        using (var about = new AboutForm(_ => Task.FromResult<ReleaseUpdate?>(new(new Version(99, 0, 0), "v99", new Uri(AppInfo.RepositoryUrl + "/releases/tag/v99")))))
        {
            about.Show(); Application.DoEvents();
            check(All(about).OfType<Label>().Any(l => l.Text.Contains(AppInfo.Version.ToString(3))) && All(about).OfType<LinkLabel>().Any(l => l.Text == L10n.Get("OpenRelease") && l.Enabled), "about dialog shows assembly version and newer release link");
            about.Close();
        }
        using (var about = new AboutForm(_ => Task.FromException<ReleaseUpdate?>(new HttpRequestException("Offline"))))
        {
            about.Show(); Application.DoEvents();
            check(All(about).OfType<Label>().Any(l => l.Text == L10n.Get("UpdateFailed")) && All(about).OfType<Button>().Any(b => b.Text == L10n.Get("CheckUpdates") && b.Enabled), "offline update check remains retryable without blocking the app");
            about.Close();
        }
        L10n.SetLanguage("en");
        check(L10n.Backend("Compressione del database") == "Compressing the database" && L10n.Backend("Data di nascita non disponibile") == "Birth date unavailable", "progress and player warnings use English resources");
        check(L10n.Backend("Test.fm: Identita' non verificata univocamente (ID, nome e nascita)") == "Test.fm: Identity could not be uniquely verified (ID, name and birth date)", "history warning translation preserves filenames");
        L10n.SetLanguage("it");
    }
    private static void Ui(Action<bool, string> check, string directory)
    {
        L10n.SetLanguage("it");
        using var main = new MainForm("python", directory); main.Show(); Application.DoEvents();
        var language = All(main).OfType<ComboBox>().Single(c => c.Name == "LanguageSelector");
        var filters = All(main).OfType<SearchOptions>().Single();
        var age = All(filters).OfType<OptionalRangeFilter>().Single(c => c.Text == "Età (anni)");
        age.Controls[0].Controls.OfType<CheckBox>().Single().Checked = true;
        var request = filters.Request("test", null, null);
        var player = new PlayerDetail
        {
            Player = new PlayerRow { Uid = 99, Name = "Portiere", Team = "Contratto", Ca = 100, Pa = 150, Birth = "2000-01-01" },
            RawPa = 150, BirthEditable = true,
            Attributes = [new EditableField { Index = 0, Key = "technique", Value = 75 }],
            Positions = [new EditableField { Index = 0, Key = "MC", Value = 20 }]
        };
        ((Dictionary<uint, PlayerRow>)typeof(MainForm).GetField("catalogByUid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(main)!).Add(99, player.Player);
        typeof(MainForm).GetMethod("SetDetail", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(main, [player]);
        player.Attributes[0].Value = 80;
        language.SelectedIndex = 1; Application.DoEvents();
        check(L10n.Language == "en" && AppSettings.Load(directory).Language == "en" && All(main).OfType<Button>().Any(b => b.Text == "Open save") && All(main).OfType<TabPage>().Any(t => t.Text == "1  Player search"), "language select updates interface and persists preference immediately");
        check(All(main).OfType<TabPage>().Any(t => t.Text == "4  Comparison (1)"), "comparison pool count tab caption changes language");
        check(player.Attributes[0].Value == 80 && player.Attributes[0].Label == "Technique" && All(main).OfType<Label>().Any(l => l.Text == "Portiere") && All(main).OfType<Label>().Any(l => l.Text == "Contratto") && All(main).OfType<Label>().Any(l => l.Text.StartsWith("Public ID:")), "language switch preserves unsaved values and player/team names while translating identity labels");
        check(All(main).OfType<CheckBox>().All(c => c.Text != "Etichette in italiano") && filters.Request("test", null, null).AgeMin == request.AgeMin, "language select replaces checkbox and preserves active filters");
        check(All(filters).OfType<GroupBox>().Any(g => g.Text == "Age (years)") && All(filters).OfType<ComboBox>().SelectMany(c => c.Items.Cast<object>()).Any(i => i.ToString() == "Expires within 6 months"), "filter captions and dropdown choices are localized");
        check(All(main).OfType<DataGridView>().Any(g => g.Columns.Cast<DataGridViewColumn>().Any(c => c.HeaderText == "Main position")) && All(main).OfType<TextBox>().Any(t => t.PlaceholderText == "Filter teams, e.g. tri"), "column headers and search placeholders are localized");
        check(All(main).OfType<HistoryPanel>().SelectMany(All).OfType<Button>().Any(b => b.Text == "Add saves…") && All(main).OfType<ComparisonPanel>().SelectMany(All).OfType<CheckBox>().Any(c => c.Text == "Show pitches"), "history and comparison static controls update together");
        language.SelectedIndex = 0; Application.DoEvents();
        check(AppSettings.Load(directory).Language == "it" && All(main).OfType<Button>().Any(b => b.Text == "Apri salvataggio") && age.Text == "Età (anni)", "language switch back restores Italian without clearing filters");
        All(main).OfType<TabControl>().First().SelectedIndex = 1; Application.DoEvents();
        var copies = All(main).OfType<CopyValueButton>().ToDictionary(b => b.Name);
        check(copies["CopyPlayerName"].Enabled && !copies["CopyPublicId"].Enabled && copies["CopyInternalId"].Enabled, "copy buttons respect missing public IDs");
        Clipboard.SetText("sentinel"); copies["CopyPlayerName"].PerformClick();
        check(Clipboard.GetText() == "Portiere", "copy name preserves the original player text");
        copies["CopyInternalId"].PerformClick();
        check(Clipboard.GetText() == "99", "copy internal ID writes only the full numeric ID");
        player.Player.Id = 4294967295;
        typeof(MainForm).GetMethod("UpdatePlayerIdentity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(main, null);
        copies["CopyPublicId"].PerformClick();
        check(Clipboard.GetText() == "4294967295", "copy public ID preserves every digit without labels or formatting");
        player.Player.Name = "João 李"; copies["CopyPlayerName"].PerformClick();
        check(Clipboard.GetText() == "João 李", "copy name preserves Unicode characters");
        player.Attributes[0].Value = 75; main.Close();
        L10n.SetLanguage("en");
        using var startup = new RuntimeSetupForm();
        check(All(startup).OfType<Button>().Any(b => b.Text == "Install Python 3.13"), "startup preparation uses saved language before Python checks");
        using var englishMain = new MainForm("python", directory);
        check(All(englishMain).OfType<Button>().Any(b => b.Text == "Open save"), "English interface initializes without an intermediate Italian layout");
        L10n.SetLanguage("it");
    }
}
