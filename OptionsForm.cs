namespace FM26SimpleEditor;

public sealed class OptionsForm : Form
{
    private sealed record LanguageChoice(string Id, string Label) { public override string ToString() => Label; }
    private readonly ComboBox language = new() { Name = "LanguageSelector", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown count = new() { Name = "RecommendationCount", Minimum = 1, Maximum = 100, Dock = DockStyle.Fill };
    public AppSettings Settings => new((language.SelectedItem as LanguageChoice)?.Id ?? "it", (int)count.Value);
    public OptionsForm(AppSettings settings)
    {
        Text = L10n.Get("OptionsTitle"); Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi; FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent; MinimizeBox = MaximizeBox = false;
        ClientSize = new Size(510, 190);
        language.Items.AddRange([new LanguageChoice("it", "Italiano"), new LanguageChoice("en", "English")]);
        language.SelectedIndex = settings.Language == "en" ? 1 : 0;
        count.Value = Math.Clamp(settings.RecommendationCount, 1, 100);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 4 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text = L10n.Get("LanguageLabel"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        root.Controls.Add(language, 1, 0);
        root.Controls.Add(new Label { Text = L10n.Get("RecommendationCountLabel"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        root.Controls.Add(count, 1, 1);
        var hint = new Label { Text = L10n.Get("RecommendationCountHint"), AutoSize = true, MaximumSize = new Size(470, 0), ForeColor = Color.DimGray };
        root.Controls.Add(hint, 0, 2); root.SetColumnSpan(hint, 2);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Name = "CancelOptions", Text = L10n.Get("OptionsCancel"), AutoSize = true, DialogResult = DialogResult.Cancel };
        var save = new Button { Name = "SaveOptions", Text = L10n.Get("OptionsSave"), AutoSize = true, DialogResult = DialogResult.OK };
        buttons.Controls.AddRange([cancel, save]); root.Controls.Add(buttons, 0, 3); root.SetColumnSpan(buttons, 2);
        AcceptButton = save; CancelButton = cancel; Controls.Add(root);
    }
}
