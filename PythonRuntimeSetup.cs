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
        catch (Exception exc) when (exc is Win32Exception or TimeoutException or InvalidOperationException) { log.Report("Launcher Python non disponibile; controllo le installazioni rilevate."); }
        foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(path)) continue;
            log.Report("Verifica Python: " + path);
            try
            {
                var info = await ProbeAsync(path,token);
                if (info.Supported && info.Error is null) return info;
                log.Report(info.Error ?? $"Python {info.Version} non compatibile: serve almeno 3.12.");
            }
            catch (Exception exc) when (exc is Win32Exception or TimeoutException or InvalidOperationException or JsonException)
            { log.Report("Interprete non utilizzabile: " + exc.Message); }
        }
        return null;
    }

    public static async Task<string> PrepareAsync(PythonRuntimeInfo detected, IProgress<string> log, CancellationToken token)
    {
        if (detected.Ready) return detected.Executable;
        log.Report("Dipendenze mancanti o non utilizzabili: " + string.Join("; ",detected.Missing));
        Directory.CreateDirectory(Root);
        using var lease = await AcquireLockAsync(log,token);
        var version = Version.Parse(detected.Version);
        string environment = Path.Combine(Root,$"python-{version.Major}.{version.Minor}");
        string executable = Path.Combine(environment,"Scripts","python.exe");
        if (File.Exists(executable))
        {
            try { if ((await ProbeAsync(executable,token)).Ready) return executable; }
            catch (Exception exc) when (exc is Win32Exception or TimeoutException or InvalidOperationException or JsonException) { log.Report("Riparazione dell’ambiente dedicato: " + exc.Message); }
        }
        string baseExecutable = File.Exists(detected.BaseExecutable) ? detected.BaseExecutable : detected.Executable;
        log.Report("Preparazione dell’ambiente dedicato: " + environment);
        await RequireSuccessAsync(baseExecutable,["-E","-X","utf8","-m","venv",environment],TimeSpan.FromMinutes(3),log,token);
        log.Report("Verifica pip nell’ambiente dedicato…");
        var pip = await RunAsync(executable,["-E","-X","utf8","-m","pip","--version"],TimeSpan.FromSeconds(20),null,token);
        if (pip.Code!=0)
            await RequireSuccessAsync(executable,["-E","-X","utf8","-m","ensurepip","--upgrade"],TimeSpan.FromMinutes(2),log,token);
        log.Report("Installazione delle dipendenze da PyPI; attendere…");
        // Only the editor's environment is modified; global/user packages are untouched.
        await RequireSuccessAsync(executable,["-E","-X","utf8","-m","pip","--isolated","install","--index-url","https://pypi.org/simple","--disable-pip-version-check","--no-input","--retries","2","--timeout","30","--upgrade","--force-reinstall","--requirement",Requirements],TimeSpan.FromMinutes(15),log,token);
        log.Report("Controllo finale delle versioni e degli import…");
        var ready = await ProbeAsync(executable,token);
        if (!ready.Ready) throw new InvalidOperationException(ready.Error ?? "Dipendenze ancora non utilizzabili: " + string.Join("; ",ready.Missing));
        return ready.Executable;
    }

    public static async Task InstallPythonAsync(IProgress<string> log, CancellationToken token)
    {
        log.Report("Installazione Python 3.13 tramite WinGet, per l’utente corrente…");
        await RequireSuccessAsync("winget",["install","--id","Python.Python.3.13","--exact","--source","winget","--scope","user","--silent","--accept-package-agreements","--accept-source-agreements","--disable-interactivity"],TimeSpan.FromMinutes(15),log,token);
        log.Report("Python installato. Ricerca del nuovo interprete…");
    }

    private static void CheckFiles()
    {
        if (!File.Exists(Probe) || !File.Exists(Requirements))
            throw new InvalidOperationException("Mancano runtime_probe.py o requirements.txt accanto all’eseguibile. Ricompila o estrai il progetto completo.");
    }
    private static async Task<PythonRuntimeInfo> ProbeAsync(string executable, CancellationToken token)
    {
        var result = await RunAsync(executable,["-E","-X","utf8",Probe,Requirements],TimeSpan.FromSeconds(20),null,token);
        if (result.Code!=0) throw new InvalidOperationException("Verifica Python fallita: " + result.Error.Trim());
        return JsonSerializer.Deserialize<PythonRuntimeInfo>(result.Output.Trim(),JsonOptions) ?? throw new InvalidOperationException("Risposta Python non valida");
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
            { log.Report("Attendo un’altra installazione dell’editor in corso…"); await Task.Delay(500,token); }
        }
    }
    private static async Task RequireSuccessAsync(string executable, string[] arguments, TimeSpan timeout, IProgress<string> log, CancellationToken token)
    {
        var result = await RunAsync(executable,arguments,timeout,log,token);
        if (result.Code!=0) throw new InvalidOperationException($"Operazione non riuscita (codice {result.Code}).\n" + Tail(result.Error.Length>0 ? result.Error : result.Output));
    }
    private static string Tail(string text) => text.Length>6000 ? text[^6000..] : text;
    private static async Task<CommandResult> RunAsync(string executable, string[] arguments, TimeSpan timeout, IProgress<string>? log, CancellationToken token)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true, StandardOutputEncoding=Encoding.UTF8, StandardErrorEncoding=Encoding.UTF8, WorkingDirectory=AppContext.BaseDirectory };
        start.Environment["PYTHONIOENCODING"]="utf-8";
        start.Environment["PYTHONUNBUFFERED"]="1";
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo=start };
        if (!process.Start()) throw new InvalidOperationException("Impossibile avviare " + executable);
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
            throw new TimeoutException("Tempo massimo superato per " + Path.GetFileName(executable));
        }
        return new(process.ExitCode,output.ToString(),error.ToString());
    }
}
