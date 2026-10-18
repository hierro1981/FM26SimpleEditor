using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FM26SimpleEditor;

public sealed class BackendClient : IDisposable
{
    private Process? process;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly StringBuilder errors = new();
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private void EnsureStarted()
    {
        if (process is { HasExited: false }) return;
        process?.Dispose();
        errors.Clear();
        var executable = Environment.GetEnvironmentVariable("FM26_PYTHON") ?? "python";
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = AppContext.BaseDirectory
        };
        start.Environment["PYTHONIOENCODING"] = "utf-8";
        start.Environment["PYTHONUNBUFFERED"] = "1";
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "editor.py"));
        try
        {
            process = new Process { StartInfo = start };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is null) return;
                lock (errors)
                {
                    errors.AppendLine(args.Data);
                    if (errors.Length > 32000) errors.Remove(0, errors.Length - 16000);
                }
            };
            if (!process.Start()) throw new InvalidOperationException("Avvio Python non riuscito");
            process.BeginErrorReadLine();
        }
        catch (Exception exc)
        {
            throw new InvalidOperationException("Python non disponibile. Installa Python 3.12 o successivo e le dipendenze indicate nel README. " + exc.Message, exc);
        }
    }

    public async Task<T> Send<T>(object request, Action<BackendProgress>? onProgress = null)
    {
        await gate.WaitAsync();
        try
        {
            EnsureStarted();
            await process!.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request, Options));
            await process.StandardInput.FlushAsync();
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line is null)
                {
                    string detail;
                    lock (errors) detail = errors.ToString().Trim();
                    throw new InvalidOperationException("Il lettore del salvataggio si è chiuso. " + detail);
                }
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (root.TryGetProperty("event",out var kind))
                {
                    if (kind.GetString() != "progress") throw new InvalidOperationException("Evento inatteso dal lettore");
                    var update = root.Deserialize<BackendProgress>(Options);
                    if (update is not null) onProgress?.Invoke(update);
                    continue;
                }
                if (!root.GetProperty("ok").GetBoolean())
                    throw new InvalidOperationException(root.GetProperty("error").GetString());
                return root.GetProperty("data").Deserialize<T>(Options) ?? throw new InvalidOperationException("Risposta vuota dal lettore");
            }
        }
        finally { gate.Release(); }
    }

    public void Dispose()
    {
        if (process is not null)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            process.Dispose();
        }
        gate.Dispose();
    }
}
