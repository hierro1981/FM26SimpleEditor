namespace FM26SimpleEditor;

public sealed record PlayerChoice(uint Uid, string Label, string SearchText)
{
    public override string ToString() => Label;
}

/// <summary>A committed selection plus a separate, searchable popup. Typing never commits.</summary>
public sealed class SearchablePlayerPicker : UserControl
{
    private readonly Button trigger = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly ToolStripDropDown popup = new() { Padding = System.Windows.Forms.Padding.Empty, AutoClose = true };
    private readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "Cerca nome, squadra o ID…" };
    private readonly ListBox results = new() { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true };
    private readonly Label count = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly TableLayoutPanel content = new() { ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
    private readonly ToolStripControlHost host;
    private List<PlayerChoice> items = [];
    public PlayerChoice? SelectedItem { get; private set; }
    public event EventHandler? SelectedIndexChanged;

    public SearchablePlayerPicker()
    {
        Width = 320; Height = 32;
        AccessibleName = "Giocatore di confronto, elenco cercabile";
        trigger.AccessibleName = AccessibleName;
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); content.RowStyles.Add(new RowStyle(SizeType.Percent,100)); content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(search,0,0); content.Controls.Add(results,0,1); content.Controls.Add(count,0,2);
        host = new ToolStripControlHost(content) { AutoSize = false, Margin = System.Windows.Forms.Padding.Empty, Padding = System.Windows.Forms.Padding.Empty };
        popup.Items.Add(host); Controls.Add(trigger);
        trigger.Click += (_, _) => { if (popup.Visible) popup.Close(); else OpenPopup(); };
        search.TextChanged += (_, _) => Filter();
        search.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Down) { if (results.Items.Count>0) { results.Focus(); results.SelectedIndex = Math.Max(0,results.SelectedIndex); } args.SuppressKeyPress=true; }
            else if (args.KeyCode == Keys.Enter) { Commit(); args.SuppressKeyPress=true; }
            else if (args.KeyCode == Keys.Escape) { popup.Close(); trigger.Focus(); args.SuppressKeyPress=true; }
        };
        results.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Enter) { Commit(); args.SuppressKeyPress=true; }
            else if (args.KeyCode == Keys.Escape) { popup.Close(); trigger.Focus(); args.SuppressKeyPress=true; }
        };
        results.MouseClick += (_, args) => { if (results.IndexFromPoint(args.Location)>=0) Commit(); };
        trigger.KeyDown += (_, args) =>
        {
            if (args.KeyCode is Keys.F4 or Keys.Down) { OpenPopup(); args.SuppressKeyPress=true; }
        };
        RefreshText();
    }
    public void SetItems(List<PlayerChoice> choices, uint? preferred)
    {
        uint? before = SelectedItem?.Uid;
        items = choices;
        SelectedItem = choices.FirstOrDefault(item => item.Uid == preferred) ?? choices.FirstOrDefault();
        RefreshText();
        if (popup.Visible) Filter();
        if (before != SelectedItem?.Uid) SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
    }
    private void RefreshText()
    {
        trigger.Text = (SelectedItem?.Label ?? "Scegli un giocatore…") + "  ▾";
        trigger.AccessibleDescription = SelectedItem?.Label ?? "Apri e digita per cercare";
    }
    private void OpenPopup()
    {
        if (popup.Visible) return;
        content.Font = Font;
        float scale = DeviceDpi/96f;
        var screen = Screen.FromControl(this).WorkingArea;
        host.Size = new Size(Math.Max(220,Math.Min((int)(530*scale),screen.Width-24)),Math.Max(150,Math.Min((int)(350*scale),screen.Height-60)));
        content.Size = host.Size;
        search.Clear(); Filter();
        popup.Show(trigger,new Point(0,trigger.Height));
        search.Focus();
    }
    private void Filter()
    {
        string text = search.Text.Trim();
        uint? highlighted = (results.SelectedItem as PlayerChoice)?.Uid ?? SelectedItem?.Uid;
        var filtered = items.Where(item => text.Length==0 || item.SearchText.Contains(text,StringComparison.OrdinalIgnoreCase) || item.Label.Contains(text,StringComparison.OrdinalIgnoreCase)).ToList();
        results.BeginUpdate(); results.Items.Clear(); results.Items.AddRange(filtered.Cast<object>().ToArray());
        int index = filtered.FindIndex(item => item.Uid==highlighted);
        results.SelectedIndex = filtered.Count==0 ? -1 : index>=0 ? index : 0;
        results.EndUpdate();
        count.Text = filtered.Count==0 ? "Nessun giocatore corrispondente" : $"{filtered.Count} giocatori · Invio o clic per scegliere · Esc per annullare";
    }
    private void Commit()
    {
        if (results.SelectedItem is not PlayerChoice choice) return;
        bool changed = SelectedItem?.Uid != choice.Uid;
        SelectedItem = choice; RefreshText(); popup.Close(); trigger.Focus();
        if (changed) SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
    }
    protected override void Dispose(bool disposing) { if (disposing) popup.Dispose(); base.Dispose(disposing); }
}
