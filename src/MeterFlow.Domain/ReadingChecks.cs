namespace MeterFlow.Domain;

public static class ReadingChecks
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public static IEnumerable<(Ean Ean, DateTimeOffset Moment)> FindDuplicates(IEnumerable<Reading> readings) =>
        readings
            .GroupBy(r => (r.Ean, r.Moment))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

    public static IEnumerable<(Ean Ean, DateTimeOffset Moment)> FindGaps(IEnumerable<Reading> readings)
    {
        foreach (var meter in readings.GroupBy(r => r.Ean))
        {
            var moments = meter.Select(r => r.Moment).Order().ToList();
            for (int i = 1; i < moments.Count; i++)
            {
                for (var missing = moments[i - 1] + Interval; missing < moments[i]; missing += Interval)
                {
                    yield return (meter.Key, missing);
                }
            }
        }
    }
}
