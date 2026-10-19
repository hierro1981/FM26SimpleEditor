using System.Collections;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace FM26SimpleEditor;

public static class L10n
{
    private static readonly ResourceManager Resources = new("FM26SimpleEditor.Resources.Strings", typeof(L10n).Assembly);
    private static readonly ConditionalWeakTable<Control, List<Action<bool>>> Bindings = new();
    private static readonly Dictionary<string, (string Key, object?[] Arguments)> Formatted = new(StringComparer.Ordinal);
    private static Dictionary<string, string>? reverse;
    private static IReadOnlyList<string>? keys;
    private static List<(string Key, Regex Pattern, int Fields)>? backendPatterns;
    public static string Language { get; private set; } = "it";
    public static bool IsItalian => Language == "it";
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(IsItalian ? "it-IT" : "en-GB");
    public static void SetLanguage(string language)
    {
        Language = language == "en" ? "en" : "it";
        CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = Culture;
    }
    public static string Get(string key, bool? italian = null) => Resources.GetString(key,
        CultureInfo.GetCultureInfo((italian ?? IsItalian) ? "it" : "en")) ?? throw new MissingManifestResourceException(key);
    public static string Format(string key, params object?[] arguments) => FormatIn(IsItalian, key, arguments);
    public static string FormatIn(bool italian, string key, params object?[] arguments)
    {
        string text = string.Format(CultureInfo.GetCultureInfo(italian ? "it-IT" : "en-GB"), Get(key, italian), arguments);
        if (Formatted.Count > 4096) Formatted.Clear();
        Formatted[text] = (key, arguments);
        return text;
    }
    public static IReadOnlyList<string> Keys => keys ??= Resources.GetResourceSet(CultureInfo.GetCultureInfo("it"), true, true)!
        .Cast<DictionaryEntry>().Select(e => (string)e.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
    private static string? KeyFor(string text)
    {
        if (reverse is null)
        {
            reverse = new(StringComparer.Ordinal);
            foreach (string key in Keys)
                foreach (bool it in new[] { true, false })
                    if (!Get(key, it).Contains('{')) reverse.TryAdd(Get(key, it), key);
        }
        return reverse.GetValueOrDefault(text);
    }
    public static string Translate(string text, bool? italian = null)
    {
        if (Formatted.TryGetValue(text, out var f)) return FormatIn(italian ?? IsItalian, f.Key, f.Arguments);
        return KeyFor(text) is string key ? Get(key, italian) : text;
    }
    // Capture only static captions before player data is bound. Never translate user input or names.
    public static void Capture(Control root, ToolTip? tips = null)
    {
        var bindings = Bindings.GetOrCreateValue(root);
        void Add(Func<string> read, Action<string> write)
        {
            string? key = KeyFor(read());
            if (key is not null) bindings.Add(it => { if (read() == Get(key, true) || read() == Get(key, false)) write(Get(key, it)); });
        }
        if (root is not TextBoxBase && root is not ComboBox && root is not ListControl) Add(() => root.Text, value => root.Text = value);
        Add(() => root.AccessibleName ?? "", value => root.AccessibleName = value);
        Add(() => root.AccessibleDescription ?? "", value => root.AccessibleDescription = value);
        if (root is TextBox text) Add(() => text.PlaceholderText, value => text.PlaceholderText = value);
        if (tips is not null) Add(() => tips.GetToolTip(root) ?? "", value => tips.SetToolTip(root, value));
        if (root is ComboBox combo)
            for (int i = 0; i < combo.Items.Count; i++)
            {
                int index = i;
                if (combo.Items[index] is string caption && KeyFor(caption) is string key)
                    bindings.Add(it => { if (index < combo.Items.Count && combo.Items[index] is string old && (old == Get(key, true) || old == Get(key, false))) combo.Items[index] = Get(key, it); });
            }
        if (root is DataGridView grid)
            foreach (DataGridViewColumn column in grid.Columns) Add(() => column.HeaderText, value => column.HeaderText = value);
        if (root.ContextMenuStrip is not null)
            foreach (ToolStripItem item in root.ContextMenuStrip.Items) Add(() => item.Text ?? "", value => item.Text = value);
        foreach (Control child in root.Controls) Capture(child, tips);
    }
    public static void Apply(Control root, bool? italian = null)
    {
        if (Bindings.TryGetValue(root, out var bindings)) foreach (var apply in bindings) apply(italian ?? IsItalian);
        foreach (Control child in root.Controls) Apply(child, italian);
        root.Invalidate();
    }
    public static string Backend(string text)
    {
        if (IsItalian || string.IsNullOrEmpty(text)) return text;
        if (backendPatterns is null)
        {
            backendPatterns = [];
            foreach (string key in Keys.Where(k => k.StartsWith("Backend_", StringComparison.Ordinal)))
            {
                string source = Get(key, true);
                var fields = Regex.Matches(source, @"\{(\d+)\}");
                string pattern = Regex.Escape(source);
                foreach (Match field in fields) pattern = pattern.Replace(Regex.Escape(field.Value), "(?<v" + field.Groups[1].Value + ">.*?)");
                backendPatterns.Add((key, new Regex("\\A" + pattern + "\\z", RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)), fields.Count));
            }
        }
        foreach (var template in backendPatterns)
        {
            var match = template.Pattern.Match(text);
            if (match.Success)
            {
                object?[] values = Enumerable.Range(0, template.Fields).Select(i => (object?)match.Groups["v" + i].Value).ToArray();
                return string.Format(Culture, Get(template.Key, false), values);
            }
        }
        int separator = text.IndexOf(": ", StringComparison.Ordinal);
        if (separator >= 0)
        {
            string tail = text[(separator + 2)..];
            string translated = Backend(tail);
            if (translated != tail) return text[..(separator + 2)] + translated;
        }
        return Translate(text);
    }
}
