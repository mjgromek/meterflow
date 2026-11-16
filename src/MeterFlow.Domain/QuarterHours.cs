namespace MeterFlow.Domain;

public static class QuarterHours
{
    public static readonly TimeSpan Length = TimeSpan.FromMinutes(15);

    public static IEnumerable<(Ean Ean, DateTimeOffset Start, decimal Kwh)> Aggregate(IEnumerable<Reading> readings) =>
        readings
            .GroupBy(r => (r.Ean, Start: Floor(r.Moment)))
            .Select(g => (g.Key.Ean, g.Key.Start, g.Sum(r => r.Kwh)));

    public static IEnumerable<DateTimeOffset> ForLocalDay(DateOnly day, TimeZoneInfo zone)
    {
        var end = LocalMidnight(day.AddDays(1), zone);
        for (var start = LocalMidnight(day, zone); start < end; start += Length)
        {
            yield return start;
        }
    }

    private static DateTimeOffset Floor(DateTimeOffset moment) =>
        new(moment.UtcTicks - moment.UtcTicks % Length.Ticks, TimeSpan.Zero);

    private static DateTimeOffset LocalMidnight(DateOnly day, TimeZoneInfo zone)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue);
        return new(midnight, zone.GetUtcOffset(midnight));
    }
}
