using System.Diagnostics;

namespace FM26SimpleEditor;

public sealed class RuntimeSetupForm : Form
{
    private readonly Label status = new() { Text = "Verifica Python e dipendenze…", AutoSize = true, Dock = DockStyle.Fill };
    private readonly TextBox details = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new() { Style = ProgressBarStyle.Marquee, Dock = DockStyle.Fill, MarqueeAnimationSpeed = 25 };
    private readonly Button install = new() { Text = "Installa Python 3.13", AutoSize = true, Visible = false };
    private readonly Button retry = new() { Text = "Riprova", AutoSize = true };
    private readonly Button website = new() { Text = "Sito ufficiale Python", AutoSize = true };
    private readonly Button cancel = new() { Text = "Esci", AutoSize = true };
    private CancellationTokenSource? cancellation;
    private bool busy;
    private bool closeRequested;

    public string? PythonExecutable { get; private set; }

    public RuntimeSetupForm()
    {
        using var iconStream = typeof(RuntimeSetupForm).Assembly.GetManifestResourceStream("FM26SimpleEditor.App.ico");
        if (iconStream is not null) Icon = new Icon(iconStream);
        Text = "FM26 Simple Editor · Preparazione all’avvio";
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 430);
        MinimumSize = new Size(540, 350);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        buttons.Controls.AddRange([install, retry, website, cancel]);
        layout.Controls.Add(status, 0, 0);
        layout.Controls.Add(progress, 0, 1);
        layout.Controls.Add(details, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        Shown += async (_, _) => await PrepareAsync(false);
        retry.Click += async (_, _) => await PrepareAsync(false);
        // Installation of Python itself always requires this explicit user action.
        install.Click += async (_, _) => await PrepareAsync(true);
        website.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(PythonRuntimeSetup.DownloadsUrl) { UseShellExecute = true }); }
            catch (Exception exc) { MessageBox.Show(this, exc.Message, "Apertura sito non riuscita", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        cancel.Click += (_, _) => Close();
        FormClosing += (_, args) =>
        {
            if (!busy) return;
            args.Cancel = true;
            closeRequested = true;
            cancel.Enabled = false;
            status.Text = "Interruzione della preparazione…";
            cancellation?.Cancel();
        };
    }

    private async Task PrepareAsync(bool installPython)
    {
        if (busy) return;
        busy = true;
        retry.Enabled = install.Enabled = false;
        progress.Visible = true;
        status.Text = installPython ? "Installazione Python…" : "Verifica Python e dipendenze…";
        cancellation = new CancellationTokenSource();
        IProgress<string> log = new Progress<string>(line =>
        {
            if (IsDisposed) return;
            if (details.TextLength > 60000) details.Text = details.Text[^30000..];
            details.AppendText(line + Environment.NewLine);
        });
        try
        {
            if (installPython) await PythonRuntimeSetup.InstallPythonAsync(log, cancellation.Token);
            var detected = await PythonRuntimeSetup.FindAsync(log, cancellation.Token);
            if (detected is null)
            {
                status.Text = "Python 3.12 o successivo non trovato.";
                log.Report("Installa Python 3.13 con il pulsante (WinGet, internet e accettazione delle condizioni dei pacchetti), oppure dal sito ufficiale. Poi premi Riprova. Se WinGet non è disponibile usa il sito ufficiale.");
                install.Visible = true;
            }
            else
            {
                install.Visible = false;
                status.Text = detected.Ready ? "Python pronto." : "Preparazione delle dipendenze…";
                PythonExecutable = await PythonRuntimeSetup.PrepareAsync(detected, log, cancellation.Token);
            }
        }
        catch (OperationCanceledException) { status.Text = "Preparazione interrotta."; }
        catch (Exception exc)
        {
            status.Text = "Preparazione non riuscita. Consulta i dettagli e premi Riprova.";
            log.Report(exc.Message);
        }
        finally
        {
            busy = false;
            cancellation.Dispose();
            cancellation = null;
            retry.Enabled = install.Enabled = true;
            progress.Visible = false;
        }
        if (closeRequested) { Close(); return; }
        if (PythonExecutable is not null) { DialogResult = DialogResult.OK; Close(); }
    }
}
