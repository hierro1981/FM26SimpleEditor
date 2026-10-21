using System.Diagnostics;

namespace FM26SimpleEditor;

public sealed class TacticalCriteriaForm : Form
{
    public TacticalCriteriaForm(IReadOnlyList<SquadTarget> targets)
    {
        Text = L10n.Get("TacticalCriteria"); Width = 900; Height = 650; MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterParent;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var note = new Label { Text = L10n.Get("TacticalMethod"), AutoSize = true, Dock = DockStyle.Fill, MaximumSize = new Size(820, 0) };
        var table = new DataGridView { Name = "TacticalCriteriaTable", Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, ScrollBars = ScrollBars.Both, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.Get("TacticalTarget"), FillWeight = 55, MinimumWidth = 200 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.Get("Text_51df0484073a"), FillWeight = 35, MinimumWidth = 130 });
        table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.Get("TacticalWeight"), FillWeight = 10, MinimumWidth = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        foreach (var target in targets.Distinct())
            foreach (var field in TacticalRoles.Weights(target).OrderByDescending(f => f.Value).ThenBy(f => Labels.Attribute(f.Key, L10n.IsItalian), Labels.Comparer(L10n.IsItalian)))
                table.Rows.Add(TacticalRoles.Label(target), Labels.Attribute(field.Key, L10n.IsItalian), field.Value);
        foreach (DataGridViewColumn col in table.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
        table.ClearSelection();
        var links = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        void AddLink(string key, string url)
        {
            var link = new LinkLabel { Text = L10n.Get(key), AutoSize = true, Margin = new Padding(6) };
            link.LinkClicked += (_, _) =>
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch (Exception exc) when (exc is System.ComponentModel.Win32Exception or InvalidOperationException) { MessageBox.Show(this, exc.Message, Text); }
            };
            links.Controls.Add(link);
        }
        AddLink("TacticalOfficialSource", TacticalRoles.OfficialSource); AddLink("TacticalCatalogueSource", TacticalRoles.CatalogueSource);
        root.Controls.Add(note, 0, 0); root.Controls.Add(table, 0, 1); root.Controls.Add(links, 0, 2); Controls.Add(root);
        root.SizeChanged += (_, _) => note.MaximumSize = new Size(Math.Max(100, root.ClientSize.Width - 30), 0);
    }
}
