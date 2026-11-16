using MeterFlow.Domain;

namespace MeterFlow.Domain.Tests;

public class ReadingTests
{
    private static readonly Ean Meter = Ean.Parse("123456789012345675");
    private static readonly DateTimeOffset Moment = new(2026, 11, 16, 10, 5, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Readings_with_the_same_values_are_equal()
    {
        Assert.Equal(new Reading(Meter, Moment, 1.5m), new Reading(Meter, Moment, 1.5m));
    }

    [Fact]
    public void Readings_with_different_kwh_are_not_equal()
    {
        Assert.NotEqual(new Reading(Meter, Moment, 1.5m), new Reading(Meter, Moment, 1.6m));
    }

    [Fact]
    public void Readings_at_the_same_local_time_in_different_offsets_are_not_equal()
    {
        Reading summer = new(Meter, new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)), 1.5m);
        Reading winter = new(Meter, new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(1)), 1.5m);

        Assert.NotEqual(summer, winter);
    }
}
