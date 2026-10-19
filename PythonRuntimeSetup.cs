using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace FM26SimpleEditor;

public sealed class PythonRuntimeInfo
{
    public string Executable { get; set; } = "";
    public string BaseExecutable { get; set; } = "";
    public string Version { get; set; } = "";
    public bool Supported { get; set; }
    public bool Ready { get; set; }
    public bool HasPip { get; set; }
    public List<string> Missing { get; set; } = [];
    public string? Error { get; set; }
}

public static class PythonRuntimeSetup
{
    public const string DownloadsUrl = "https://www.python.org/downloads/windows/";
    private static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FM26SimpleEditor", "runtimes");
    private static readonly string Probe = Path.Combine(AppContext.BaseDirectory, "runtime_probe.py");
    private static readonly string Requirements = Path.Combine(AppContext.BaseDirectory, "requirements.txt");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private sealed record CommandResult(int Code, string Output, string Error);

    public static async Task<PythonRuntimeInfo?> FindAsync(IProgress<string> log, CancellationToken token)
    {
        CheckFiles();
        var paths = new List<string>();
        string? configured = Environment.GetEnvironmentVariable("FM26_PYTHON");
        if (!string.IsNullOrWhiteSpace(configured)) paths.Add(configured.Trim().Trim('"'));
        if (Directory.Exists(Root))
            paths.AddRange(Directory.EnumerateDirectories(Root,"python-*").OrderByDescending(path => path,StringComparer.OrdinalIgnoreCase).Select(path => Path.Combine(path,"Scripts","python.exe")));
        AddRegistryPaths(paths);
        string localPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python");
        if (Directory.Exists(localPrograms))
            paths.AddRange(Directory.EnumerateDirectories(localPrograms,"Python*").OrderByDescending(path => path,StringComparer.OrdinalIgnoreCase).Select(path => Path.Combine(path,"python.exe")));
        foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator,StringSplitOptions.RemoveEmptyEntries))
        {
            // Skip Store aliases: checking them can open the Store rather than an interpreter.
            string directory = folder.Trim().Trim('"');
            if (directory.Contains("WindowsApps",StringComparison.OrdinalIgnoreCase)) continue;
            foreach (string name in new[] { "python.exe", "python3.exe" }) paths.Add(Path.Combine(directory,name));
        }
        try
        {
            var launchers = await RunAsync("py",["--list-paths"],TimeSpan.FromSeconds(10),null,token);
            foreach (string line in launchers.Output.Split('\n'))
            {
                var match = Regex.Match(line,@"(?<path>[A-Za-z]:\\.*python(?:\d+(?:\.\d+)?)?\.exe)\s*$",RegexOptions.IgnoreCase);
                if (match.Success) paths.Add(match.Groups["path"].Value.Trim());
            }
        }
        catch (Exception exc) when (exc is Win32Exception or TimeoutException or InvalidOperationException) { log.Report(L10n.Get("Text_0e3fdef95375")); }
        foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(path)) continue;
            log.Report(L10n.Get("Text_bf43a8426aed") + path);
            try
            {
                var info = await ProbeAsync(path,token);
                if (info.Supported && info.Error is null) return info;
                log.Report(info.Error ?? L10n.Format("Text_3d0260e57cec", info.Version));
            }
            catch (Exception exc) when (exc is Win32Exception or TimeoutException or InvalidOperationException or JsonException)
            { log.Report(L10n.Get("Text_1a19746bb534") + exc.Message); }
        }
        return null;
    }

    public static async Task<string> PrepareAsync(PythonRuntimeInfo detected, IProgress<string> log, CancellationToken token)
    {
        if (detected.Ready) return detected.Executable;
        log.Report(L10n.Get("Text_0817a096bf96") + string.Join("; ",detected.Missing));
        Directory.CreateDirectory(Root);
        using var lease = await AcquireLockAsync(log,token);
        var version = Version.Parse(detected.Version);
        string environment = Path.Combine(Root,$"python-{version.Major}.{version.Minor}");
        string executable = Path.Combine(environment,"Scripts","python.exe");
        if (File.Exists(executable))
        {
            try { if ((await ProbeAsync(executable,token)).Ready) return executable; }
            catch (Exception exc) when (exc is Win32Exception or TimeoutException or InvalidOperationException or JsonException) { log.Report(L10n.Get("Text_103779fe3c89") + exc.Message); }
        }
        string baseExecutable = File.Exists(detected.BaseExecutable) ? detected.BaseExecutable : detected.Executable;
        log.Report(L10n.Get("Text_ed1e0716be83") + environment);
        await RequireSuccessAsync(baseExecutable,["-E","-X","utf8","-m","venv",environment],TimeSpan.FromMinutes(3),log,token);
        log.Report(L10n.Get("Text_bf7555be00c9"));
        var pip = await RunAsync(executable,["-E","-X","utf8","-m","pip","--version"],TimeSpan.FromSeconds(20),null,token);
        if (pip.Code!=0)
            await RequireSuccessAsync(executable,["-E","-X","utf8","-m","ensurepip","--upgrade"],TimeSpan.FromMinutes(2),log,token);
        log.Report(L10n.Get("Text_abfe1ebbee3f"));
        // Only the editor's environment is modified; global/user packages are untouched.
        await RequireSuccessAsync(executable,["-E","-X","utf8","-m","pip","--isolated","install","--index-url","https://pypi.org/simple","--disable-pip-version-check","--no-input","--retries","2","--timeout","30","--upgrade","--force-reinstall","--requirement",Requirements],TimeSpan.FromMinutes(15),log,token);
        log.Report(L10n.Get("Text_f934e2fe40d8"));
        var ready = await ProbeAsync(executable,token);
        if (!ready.Ready) throw new InvalidOperationException(ready.Error ?? L10n.Get("Text_ddae15dbe328") + string.Join("; ",ready.Missing));
        return ready.Executable;
    }

    public static async Task InstallPythonAsync(IProgress<string> log, CancellationToken token)
    {
        log.Report(L10n.Get("Text_5714b701231e"));
        await RequireSuccessAsync("winget",["install","--id","Python.Python.3.13","--exact","--source","winget","--scope","user","--silent","--accept-package-agreements","--accept-source-agreements","--disable-interactivity"],TimeSpan.FromMinutes(15),log,token);
        log.Report(L10n.Get("Text_4c8cf7466688"));
    }

    private static void CheckFiles()
    {
        if (!File.Exists(Probe) || !File.Exists(Requirements))
            throw new InvalidOperationException(L10n.Get("Text_7f3477fd96c4"));
    }
    private static async Task<PythonRuntimeInfo> ProbeAsync(string executable, CancellationToken token)
    {
        var result = await RunAsync(executable,["-E","-X","utf8",Probe,Requirements],TimeSpan.FromSeconds(20),null,token);
        if (result.Code!=0) throw new InvalidOperationException(L10n.Get("Text_5fe0d6b9a217") + result.Error.Trim());
        return JsonSerializer.Deserialize<PythonRuntimeInfo>(result.Output.Trim(),JsonOptions) ?? throw new InvalidOperationException(L10n.Get("Text_376a6a670a42"));
    }
    private static void AddRegistryPaths(List<string> paths)
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive,view);
                using var versions = root.OpenSubKey(@"Software\Python\PythonCore");
                if (versions is null) continue;
                foreach (string name in versions.GetSubKeyNames().OrderByDescending(name => name,StringComparer.OrdinalIgnoreCase))
                {
                    using var installed = versions.OpenSubKey(name+@"\InstallPath");
                    if (installed?.GetValue("ExecutablePath") is string exe) paths.Add(exe);
                    else if (installed?.GetValue("") is string folder) paths.Add(Path.Combine(folder,"python.exe"));
                }
            }
            catch (Exception exc) when (exc is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        }
    }
    private static async Task<FileStream> AcquireLockAsync(IProgress<string> log, CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try { return new FileStream(Path.Combine(Root,"setup.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); }
            catch (IOException) when (watch.Elapsed < TimeSpan.FromMinutes(2))
            { log.Report(L10n.Get("Text_cb2cc50c4178")); await Task.Delay(500,token); }
        }
    }
    private static async Task RequireSuccessAsync(string executable, string[] arguments, TimeSpan timeout, IProgress<string> log, CancellationToken token)
    {
        var result = await RunAsync(executable,arguments,timeout,log,token);
        if (result.Code!=0) throw new InvalidOperationException(L10n.Format("Text_75ca7ae5eaa1", result.Code) + Tail(result.Error.Length>0 ? result.Error : result.Output));
    }
    private static string Tail(string text) => text.Length>6000 ? text[^6000..] : text;
    private static async Task<CommandResult> RunAsync(string executable, string[] arguments, TimeSpan timeout, IProgress<string>? log, CancellationToken token)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true, StandardOutputEncoding=Encoding.UTF8, StandardErrorEncoding=Encoding.UTF8, WorkingDirectory=AppContext.BaseDirectory };
        start.Environment["PYTHONIOENCODING"]="utf-8";
        start.Environment["PYTHONUNBUFFERED"]="1";
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo=start };
        if (!process.Start()) throw new InvalidOperationException(L10n.Get("Text_76636614446e") + executable);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(timeout);
        var output = new StringBuilder(); var error = new StringBuilder();
        async Task PumpAsync(StreamReader reader, StringBuilder buffer)
        {
            while (await reader.ReadLineAsync(deadline.Token) is string line)
            {
                if (buffer.Length>32000) buffer.Remove(0,buffer.Length-16000);
                buffer.AppendLine(line); log?.Report(line);
            }
        }
        var stdout=PumpAsync(process.StandardOutput,output); var stderr=PumpAsync(process.StandardError,error);
        try { await process.WaitForExitAsync(deadline.Token); await Task.WhenAll(stdout,stderr); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree:true); } catch (Exception exc) when (exc is InvalidOperationException or Win32Exception) { log?.Report(exc.Message); }
            try { await Task.WhenAll(stdout,stderr); } catch (OperationCanceledException) { }
            token.ThrowIfCancellationRequested();
            throw new TimeoutException(L10n.Get("Text_d004bbfcb761") + Path.GetFileName(executable));
        }
        return new(process.ExitCode,output.ToString(),error.ToString());
    }
}

