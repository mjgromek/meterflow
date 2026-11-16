using MeterFlow.Domain;

namespace MeterFlow.Domain.Tests;

public class QuarterHoursTests
{
    private static readonly Ean Meter = Ean.Parse("123456789012345675");
    private static readonly TimeZoneInfo Amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    [Theory]
    [InlineData(2026, 3, 29, 92)]
    [InlineData(2026, 11, 16, 96)]
    [InlineData(2026, 10, 25, 100)]
    public void A_local_day_has_one_quarter_per_15_minutes_between_local_midnights(int year, int month, int day, int expected)
    {
        var quarters = QuarterHours.ForLocalDay(new DateOnly(year, month, day), Amsterdam).ToList();

        Assert.Equal(expected, quarters.Count);
        Assert.Equal(new DateTimeOffset(year, month, day, 0, 0, 0, Amsterdam.GetUtcOffset(new DateTime(year, month, day))), quarters[0]);
    }

    [Fact]
    public void Three_readings_in_one_quarter_are_summed()
    {
        var start = new DateTimeOffset(2026, 11, 16, 10, 15, 0, TimeSpan.FromHours(1));
        Reading[] readings =
        [
            new(Meter, start, 1.1m),
            new(Meter, start.AddMinutes(5), 1.2m),
            new(Meter, start.AddMinutes(10), 1.3m),
        ];

        Assert.Equal([(Meter, start, 3.6m)], QuarterHours.Aggregate(readings));
    }

    [Fact]
    public void The_repeated_02_00_on_the_last_summer_time_day_is_two_quarters()
    {
        var summer = new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.FromHours(2));
        var winter = new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.FromHours(1));
        Reading[] readings = [new(Meter, summer, 1m), new(Meter, winter, 2m)];

        Assert.Equal([(Meter, summer, 1m), (Meter, winter, 2m)], QuarterHours.Aggregate(readings));
    }

    [Fact]
    public void Aggregation_preserves_the_total()
    {
        var start = new DateTimeOffset(2026, 10, 25, 1, 50, 0, TimeSpan.FromHours(2));
        var readings = Enumerable.Range(0, 30)
            .Select(i => new Reading(Meter, start.AddMinutes(5 * i), 0.001m * (i + 1)))
            .ToList();

        Assert.Equal(readings.Sum(r => r.Kwh), QuarterHours.Aggregate(readings).Sum(q => q.Kwh));
    }
}
