namespace FM26SimpleEditor;

public static class QuickComparison
{
    public static uint? Uid(DataGridViewRow row) => row.DataBoundItem is PlayerRow player ? player.Uid :
        row.Tag is SquadRecommendation recommendation ? recommendation.Player.Uid : null;

    // Use visible order, not the reverse order of DataGridView.SelectedRows.
    public static uint[] Selected(DataGridView grid) => grid.SelectedRows.Cast<DataGridViewRow>()
        .OrderBy(row => row.Index).Select(Uid).Where(uid => uid.HasValue).Select(uid => uid!.Value).Distinct().ToArray();

    public static bool TryPair(DataGridView grid, out uint first, out uint second)
    {
        var selected = Selected(grid);
        first = selected.Length == 2 ? selected[0] : 0;
        second = selected.Length == 2 ? selected[1] : 0;
        return grid.SelectedRows.Count == 2 && selected.Length == 2;
    }

    public static void SelectContextRow(DataGridView grid, int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count || grid.Rows[rowIndex].Selected) return;
        grid.ClearSelection();
        grid.CurrentCell = grid.Rows[rowIndex].Cells[Math.Max(0, columnIndex)];
        grid.Rows[rowIndex].Selected = true;
    }
}
