namespace FM26SimpleEditor;

// Stable multi-column ordering for the two planning lists; nulls stay last in either direction.
public sealed class StaffPlanningSort(string initialField, bool descending = false)
{
    public sealed record Term(string Field, bool Descending);
    private readonly List<Term> terms = [new(initialField, descending)];
    public IReadOnlyList<Term> Terms => terms.AsReadOnly();
    public void Click(string field, bool additive, bool defaultDescending = false)
    {
        int index = terms.FindIndex(t => t.Field == field);
        var term = new Term(field, index >= 0 ? !terms[index].Descending : defaultDescending);
        if (!additive) { terms.Clear(); terms.Add(term); }
        else if (index >= 0) terms[index] = term;
        else terms.Add(term);
    }
    public IOrderedEnumerable<T> Apply<T>(IEnumerable<T> rows, Func<T,string,object?> value, Func<T,int>? priority = null)
    {
        var ordered = rows.OrderBy(row => priority?.Invoke(row) ?? 0);
        foreach (var term in terms)
        {
            ordered = ordered.ThenBy(row => value(row, term.Field) is null);
            ordered = term.Descending ? ordered.ThenByDescending(row => value(row, term.Field)) : ordered.ThenBy(row => value(row, term.Field));
        }
        return ordered;
    }
    public void UpdateHeaders(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (column.SortMode == DataGridViewColumnSortMode.NotSortable) continue;
            int index = terms.FindIndex(t => t.Field == column.Name);
            string caption = column.Tag is string key ? L10n.Get(key) : column.Name.ToUpperInvariant();
            column.HeaderText = caption + (terms.Count > 1 && index >= 0 ? $" [{index+1}]" : "");
            column.HeaderCell.SortGlyphDirection = index < 0 ? SortOrder.None : terms[index].Descending ? SortOrder.Descending : SortOrder.Ascending;
        }
    }
}
