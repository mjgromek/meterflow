namespace MeterFlow.Domain;

public static class ReadingChecks
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public static IEnumerable<(Ean Ean, DateTimeOffset Moment)> FindDuplicates(IEnumerable<Reading> readings) =>
        readings
            .GroupBy(r => (r.Ean, r.Moment))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

    /// <summary>Every 5-minute moment in [from, to) without a reading, per meter that has a reading in the window.</summary>
    public static IEnumerable<(Ean Ean, DateTimeOffset Moment)> FindGaps(IEnumerable<Reading> readings, DateTimeOffset from, DateTimeOffset to)
    {
        long step = Interval.Ticks;
        var first = new DateTimeOffset((from.UtcTicks + step - 1) / step * step, TimeSpan.Zero);
        foreach (var meter in readings.Where(r => r.Moment >= from && r.Moment < to).GroupBy(r => r.Ean))
        {
            // DateTimeOffset equality compares the instant, so offsets do not matter here.
            var present = meter.Select(r => r.Moment).ToHashSet();
            for (var moment = first; moment < to; moment += Interval)
            {
                if (!present.Contains(moment))
                {
                    yield return (meter.Key, moment);
                }
            }
        }
    }

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
