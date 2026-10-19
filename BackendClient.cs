using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FM26SimpleEditor;

public sealed class BackendClient : IDisposable
{
    private readonly string executable;

    public BackendClient(string pythonExecutable) => executable = pythonExecutable;

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
        start.ArgumentList.Add("-E");
        start.ArgumentList.Add("-X");
        start.ArgumentList.Add("utf8");
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
            if (!process.Start()) throw new InvalidOperationException(L10n.Get("Text_9d8d2a088944"));
            process.BeginErrorReadLine();
        }
        catch (Exception exc)
        {
            throw new InvalidOperationException(L10n.Get("Text_72cbceadc06a") + exc.Message, exc);
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
                    throw new InvalidOperationException(L10n.Get("Text_486f013addb0") + detail);
                }
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (root.TryGetProperty("event",out var kind))
                {
                    if (kind.GetString() != "progress") throw new InvalidOperationException(L10n.Get("Text_68663ead19d6"));
                    var update = root.Deserialize<BackendProgress>(Options);
                    if (update is not null) onProgress?.Invoke(update);
                    continue;
                }
                if (!root.GetProperty("ok").GetBoolean())
                    throw new InvalidOperationException(L10n.Backend(root.GetProperty("error").GetString() ?? ""));
                return root.GetProperty("data").Deserialize<T>(Options) ?? throw new InvalidOperationException(L10n.Get("Text_5a6045247428"));
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

