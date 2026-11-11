namespace FM26SimpleEditor;

// Shown before runtime preparation only when no valid preference was stored.
public sealed class InitialLanguageForm : Form
{
    private readonly ComboBox choices = new() { Name = "InitialLanguage", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    public string Language => choices.SelectedIndex switch { 1 => "en", 2 => "fr", 3 => "de", _ => "it" };
    public InitialLanguageForm()
    {
        Text = "Lingua / Language"; StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MinimizeBox = MaximizeBox = false;
        ClientSize = new Size(380, 155); Font = new Font("Segoe UI", 10);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = "Scegli la lingua / Choose your language", AutoSize = true, Margin = new Padding(0, 0, 0, 10) }, 0, 0);
        choices.Items.AddRange(["Italiano", "English", "Français", "Deutsch"]); choices.SelectedIndex = 0; root.Controls.Add(choices, 0, 1);
        var ok = new Button { Text = "OK", AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Bottom, DialogResult = DialogResult.OK };
        root.Controls.Add(ok, 0, 2); Controls.Add(root); AcceptButton = ok;
    }
}
