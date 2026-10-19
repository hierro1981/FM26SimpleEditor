using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace FM26SimpleEditor;

public sealed class AboutForm : Form
{
    private readonly Label updateStatus = new() { AutoSize = true, Dock = DockStyle.Fill, MaximumSize = new Size(535, 0) };
    private readonly Button check = new() { Text = L10n.Get("CheckUpdates"), AutoSize = true };
    private readonly LinkLabel release = new() { Text = L10n.Get("OpenRelease"), AutoSize = true, Enabled = false };
    private readonly CancellationTokenSource cancellation = new();
    private readonly Func<CancellationToken, Task<ReleaseUpdate?>> lookup;
    private Uri? releaseUrl;
    private bool disposedResources;
    public AboutForm(Func<CancellationToken, Task<ReleaseUpdate?>>? lookup = null)
    {
        this.lookup = lookup ?? new UpdateChecker().LatestAsync;
        Text = L10n.Get("AboutTitle"); Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi; StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(590, 290);
        using var iconStream = typeof(AboutForm).Assembly.GetManifestResourceStream("FM26SimpleEditor.App.ico");
        if (iconStream is not null) Icon = new Icon(iconStream);
        var root = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(18), ColumnCount = 1, RowCount = 7 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 6; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text = "FM26 Simple Editor", AutoSize = true, Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        root.Controls.Add(new Label { Text = L10n.Format("InstalledVersion", AppInfo.Version.ToString(3)), AutoSize = true }, 0, 1);
        root.Controls.Add(new Label { Text = L10n.Get("AboutDescription"), AutoSize = true, MaximumSize = new Size(535, 0), Margin = new Padding(3, 10, 3, 10) }, 0, 2);
        var repository = new LinkLabel { Text = AppInfo.RepositoryUrl, AutoSize = true };
        repository.LinkClicked += (_, _) => Open(new Uri(AppInfo.RepositoryUrl));
        root.Controls.Add(repository, 0, 3); root.Controls.Add(updateStatus, 0, 4);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 10, 0, 0) };
        var close = new Button { Text = L10n.Get("Close"), AutoSize = true, DialogResult = DialogResult.Cancel };
        actions.Controls.AddRange([check, release, close]); root.Controls.Add(actions, 0, 5);
        root.Controls.Add(new Label { Text = L10n.Get("UpdatePrivacy"), AutoSize = true, MaximumSize = new Size(535, 0), ForeColor = Color.DimGray, Margin = new Padding(3, 10, 3, 3) }, 0, 6);
        bool fitting = false;
        void FitHeight()
        {
            if (fitting) return;
            fitting = true;
            try
            {
                int height = root.GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
                if (height > 0 && ClientSize.Height != height) ClientSize = new Size(ClientSize.Width, height);
            }
            finally { fitting = false; }
        }
        Controls.Add(root); CancelButton = close;
        root.SizeChanged += (_, _) => FitHeight();
        updateStatus.TextChanged += (_, _) => FitHeight();
        Shown += (_, _) => FitHeight();
        check.Click += async (_, _) => await CheckAsync();
        release.LinkClicked += (_, _) => { if (releaseUrl is not null) Open(releaseUrl); };
        Shown += async (_, _) => await CheckAsync();
        FormClosed += (_, _) => { if (!disposedResources) cancellation.Cancel(); };
    }
    private async Task CheckAsync()
    {
        check.Enabled = release.Enabled = false; releaseUrl = null;
        updateStatus.Text = L10n.Get("CheckingUpdates");
        try
        {
            var latest = await lookup(cancellation.Token);
            if (IsDisposed || cancellation.IsCancellationRequested) return;
            if (latest is null) updateStatus.Text = L10n.Get("NoReleaseInfo");
            else if (latest.Version > AppInfo.Version)
            {
                releaseUrl = latest.Url; release.Enabled = true;
                updateStatus.Text = L10n.Format("UpdateAvailable", latest.Tag);
            }
            else updateStatus.Text = L10n.Format("UpToDate", latest.Tag);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exc) when (exc is HttpRequestException or OperationCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            if (!IsDisposed) updateStatus.Text = exc.Message == "RateLimit" ? L10n.Get("UpdateRateLimit") : L10n.Get("UpdateFailed");
        }
        finally { if (!IsDisposed) check.Enabled = true; }
    }
    private void Open(Uri url)
    {
        try { Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception exc) { MessageBox.Show(this, L10n.Format("OpenLinkFailed", exc.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposedResources) { disposedResources = true; cancellation.Cancel(); cancellation.Dispose(); }
        base.Dispose(disposing);
    }
}
