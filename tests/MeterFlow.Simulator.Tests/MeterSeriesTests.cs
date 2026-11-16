using MeterFlow.Domain;

namespace MeterFlow.Simulator.Tests;

public class MeterSeriesTests
{
    private static readonly Ean Meter = Ean.Parse("123456789012345675");
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Produces_one_reading_per_step_for_the_given_meter()
    {
        var readings = MeterSeries.For(Meter, Start, 288, new Random(42)).ToList();

        Assert.Equal(288, readings.Count);
        Assert.All(readings, r => Assert.Equal(Meter, r.Ean));
        Assert.Equal(Schedule.FiveMinuteSteps(Start, 288), readings.Select(r => r.Moment));
    }

    [Fact]
    public void Enumerating_twice_gives_different_values()
    {
        var series = MeterSeries.For(Meter, Start, 288, new Random(42));

        Assert.NotEqual(series.Select(r => r.Kwh).ToList(), series.Select(r => r.Kwh).ToList());
    }
}
