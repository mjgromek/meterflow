namespace MeterFlow.Simulator.Tests;

public class MeterProfileTests
{
    private static readonly DateTimeOffset Midnight = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Every_value_in_a_day_stays_within_base_and_peak_plus_noise()
    {
        var rng = new Random(42);

        foreach (var moment in Schedule.FiveMinuteSteps(Midnight, 288))
        {
            decimal kwh = MeterProfile.KwhAt(moment, rng);
            Assert.InRange(kwh, 0.040m, 0.300m);
        }
    }

    [Fact]
    public void Evening_peak_is_higher_than_early_morning()
    {
        var rng = new Random(42);

        decimal evening = MeterProfile.KwhAt(Midnight.AddHours(18), rng);
        decimal morning = MeterProfile.KwhAt(Midnight.AddHours(6), rng);

        Assert.True(evening > morning, $"18:00 = {evening}, 06:00 = {morning}");
    }

    [Fact]
    public void Same_seed_gives_the_same_value()
    {
        var moment = Midnight.AddHours(12);

        Assert.Equal(MeterProfile.KwhAt(moment, new Random(7)), MeterProfile.KwhAt(moment, new Random(7)));
    }

    [Fact]
    public void Values_are_rounded_to_three_decimals()
    {
        decimal kwh = MeterProfile.KwhAt(Midnight.AddHours(9), new Random(1));

        Assert.Equal(Math.Round(kwh, 3), kwh);
    }
}
