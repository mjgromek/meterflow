using MeterFlow.Domain;

namespace MeterFlow.Simulator.Tests;

public class FaultsTests
{
    private static readonly Ean Meter = Ean.Parse("123456789012345675");
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));
    private static readonly List<Reading> Day = MeterSeries.For(Meter, Start, 288, new Random(42)).ToList();

    [Fact]
    public void Zero_chances_leave_readings_unchanged()
    {
        Assert.Equal(Day, Faults.Inject(Day, new Random(1), 0, 0));
    }

    [Fact]
    public void Missing_chance_of_one_drops_everything()
    {
        Assert.Empty(Faults.Inject(Day, new Random(1), 1, 0));
    }

    [Fact]
    public void Duplicate_chance_of_one_repeats_every_reading_right_after_itself()
    {
        var result = Faults.Inject(Day, new Random(1), 0, 1).ToList();

        Assert.Equal(Day.SelectMany(r => new[] { r, r }), result);
    }

    [Fact]
    public void Small_chances_produce_some_gaps_and_some_duplicates()
    {
        var result = Faults.Inject(Day, new Random(1), 0.05, 0.05).ToList();

        int distinct = result.Distinct().Count();
        Assert.InRange(Day.Count - distinct, 1, 50);
        Assert.InRange(result.Count - distinct, 1, 50);
    }

    [Fact]
    public void Chance_outside_zero_to_one_is_rejected_at_the_call()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Faults.Inject(Day, new Random(1), 1.5, 0));
    }
}
