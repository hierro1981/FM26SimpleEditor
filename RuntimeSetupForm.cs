using System.Diagnostics;

namespace FM26SimpleEditor;

public sealed class RuntimeSetupForm : Form
{
    private readonly Label status = new() { Text = L10n.Get("Text_f0c0f49590d2"), AutoSize = true, Dock = DockStyle.Fill };
    private readonly TextBox details = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new() { Style = ProgressBarStyle.Marquee, Dock = DockStyle.Fill, MarqueeAnimationSpeed = 25 };
    private readonly Button install = new() { Text = L10n.Get("Text_7edc4bcca94c"), AutoSize = true, Visible = false };
    private readonly Button retry = new() { Text = L10n.Get("Text_20b5f4b8bfd5"), AutoSize = true };
    private readonly Button website = new() { Text = L10n.Get("Text_4d20e07c4d4c"), AutoSize = true };
    private readonly Button cancel = new() { Text = L10n.Get("Text_58025f3619bf"), AutoSize = true };
    private CancellationTokenSource? cancellation;
    private bool busy;
    private bool closeRequested;

    public string? PythonExecutable { get; private set; }

    public RuntimeSetupForm()
    {
        using var iconStream = typeof(RuntimeSetupForm).Assembly.GetManifestResourceStream("FM26SimpleEditor.App.ico");
        if (iconStream is not null) Icon = new Icon(iconStream);
        Text = L10n.Get("Text_72a1409756a3");
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
            catch (Exception exc) { MessageBox.Show(this, exc.Message, L10n.Get("Text_ec7570d31eef"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        cancel.Click += (_, _) => Close();
        FormClosing += (_, args) =>
        {
            if (!busy) return;
            args.Cancel = true;
            closeRequested = true;
            cancel.Enabled = false;
            status.Text = L10n.Get("Text_a60e1c5efc02");
            cancellation?.Cancel();
        };
    }

    private async Task PrepareAsync(bool installPython)
    {
        if (busy) return;
        busy = true;
        retry.Enabled = install.Enabled = false;
        progress.Visible = true;
        status.Text = installPython ? L10n.Get("Text_9fd44301d99b") : L10n.Get("Text_f0c0f49590d2");
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
                status.Text = L10n.Get("Text_34e49b06cca6");
                log.Report(L10n.Get("Text_da93783f7a81"));
                install.Visible = true;
            }
            else
            {
                install.Visible = false;
                status.Text = detected.Ready ? L10n.Get("Text_26232926a988") : L10n.Get("Text_e10ca3898158");
                PythonExecutable = await PythonRuntimeSetup.PrepareAsync(detected, log, cancellation.Token);
            }
        }
        catch (OperationCanceledException) { status.Text = L10n.Get("Text_f7a219323e1f"); }
        catch (Exception exc)
        {
            status.Text = L10n.Get("Text_fcbecb092916");
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

