using MeterFlow.Domain;

namespace MeterFlow.Domain.Tests;

public class ReadingChecksTests
{
    private static readonly Ean Meter = Ean.Parse("123456789012345675");
    private static readonly DateTimeOffset Moment = new(2026, 11, 16, 10, 5, 0, TimeSpan.FromHours(1));

    [Fact]
    public void A_repeated_meter_and_moment_is_reported_once()
    {
        Reading[] readings =
        [
            new(Meter, Moment, 1.5m),
            new(Meter, Moment, 1.7m),
            new(Meter, Moment.AddMinutes(5), 1.5m),
        ];

        Assert.Equal([(Meter, Moment)], ReadingChecks.FindDuplicates(readings));
    }

    [Fact]
    public void Distinct_readings_have_no_duplicates()
    {
        Reading[] readings =
        [
            new(Meter, Moment, 1.5m),
            new(Meter, Moment.AddMinutes(5), 1.5m),
        ];

        Assert.Empty(ReadingChecks.FindDuplicates(readings));
    }

    [Fact]
    public void Missing_moments_between_readings_are_reported()
    {
        Reading[] readings =
        [
            new(Meter, Moment, 1m),
            new(Meter, Moment.AddMinutes(15), 1m),
            new(Meter, Moment.AddMinutes(5), 1m),
            new(Meter, Moment.AddMinutes(30), 1m),
        ];

        Assert.Equal(
            [(Meter, Moment.AddMinutes(10)), (Meter, Moment.AddMinutes(20)), (Meter, Moment.AddMinutes(25))],
            ReadingChecks.FindGaps(readings));
    }

    [Fact]
    public void Gaps_are_found_per_meter()
    {
        Ean other = Ean.Parse("000000000000000000");
        Reading[] readings =
        [
            new(Meter, Moment, 1m),
            new(Meter, Moment.AddMinutes(5), 1m),
            new(other, Moment, 1m),
            new(other, Moment.AddMinutes(10), 1m),
        ];

        Assert.Equal([(other, Moment.AddMinutes(5))], ReadingChecks.FindGaps(readings));
    }

    [Fact]
    public void A_window_reports_missing_moments_at_its_edges_only_for_meters_with_readings_in_it()
    {
        Ean other = Ean.Parse("000000000000000000");
        Reading[] readings =
        [
            new(Meter, Moment.AddMinutes(5), 1m),
            new(Meter, Moment.AddMinutes(15), 1m),
            new(Meter, Moment.AddMinutes(-5), 1m),
            new(other, Moment.AddMinutes(25), 1m),
        ];

        Assert.Equal(
            [(Meter, Moment), (Meter, Moment.AddMinutes(10)), (Meter, Moment.AddMinutes(20))],
            ReadingChecks.FindGaps(readings, Moment, Moment.AddMinutes(25)));
    }

    [Fact]
    public void A_window_starting_between_moments_checks_the_5_minute_grid_from_the_next_moment()
    {
        Reading[] readings = [new(Meter, Moment.AddMinutes(5), 1m)];

        Assert.Equal(
            [(Meter, Moment.AddMinutes(10))],
            ReadingChecks.FindGaps(readings, Moment.AddMinutes(2), Moment.AddMinutes(15)));
    }

    [Fact]
    public void The_repeated_hour_at_the_end_of_summer_time_is_not_a_gap()
    {
        Reading[] readings =
        [
            new(Meter, new DateTimeOffset(2026, 10, 25, 2, 55, 0, TimeSpan.FromHours(2)), 1m),
            new(Meter, new DateTimeOffset(2026, 10, 25, 2, 0, 0, TimeSpan.FromHours(1)), 1m),
        ];

        Assert.Empty(ReadingChecks.FindGaps(readings));
    }
}
