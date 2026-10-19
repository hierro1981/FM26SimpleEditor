using System.Globalization;

namespace FM26SimpleEditor;

public static class HistoryCalendar
{
    // Empirical calendar: retained samples are completed months before the save month.
    // Return no calendar if the source/date/sequence is ambiguous; never change stored dates.
    public static IReadOnlyDictionary<int, DateTime> ReconstructMonths(HistoryResult result)
    {
        var samples = result.EmbeddedPoints;
        if (samples.Count is < 2 or > 1200 || samples.Any(p => p.Date.Length != 0) ||
            !samples.Select(p => p.Sequence).Order().SequenceEqual(Enumerable.Range(1, samples.Count))) return new Dictionary<int, DateTime>();
        var sources = samples.Select(p => p.Source).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (sources.Length != 1 || sources[0].Length == 0) return new Dictionary<int, DateTime>();
        var references = result.Points.Where(p => p.Source.Equals(sources[0], StringComparison.OrdinalIgnoreCase)).ToArray();
        if (references.Length != 1 || !DateTime.TryParseExact(references[0].Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var reference)) return new Dictionary<int, DateTime>();
        try
        {
            var first = new DateTime(reference.Year, reference.Month, 1).AddMonths(-samples.Count);
            return samples.ToDictionary(p => p.Sequence, p => first.AddMonths(p.Sequence - 1));
        }
        catch (ArgumentOutOfRangeException) { return new Dictionary<int, DateTime>(); }
    }
}
