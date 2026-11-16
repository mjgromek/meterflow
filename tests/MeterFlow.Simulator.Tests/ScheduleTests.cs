namespace MeterFlow.Simulator.Tests;

public class ScheduleTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 16, 0, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Returns_the_requested_number_of_steps_five_minutes_apart()
    {
        var steps = Schedule.FiveMinuteSteps(Start, 288).ToList();

        Assert.Equal(288, steps.Count);
        Assert.Equal(Start, steps[0]);
        Assert.All(steps.Zip(steps.Skip(1)), pair => Assert.Equal(TimeSpan.FromMinutes(5), pair.Second - pair.First));
    }

    [Fact]
    public void Zero_count_returns_no_steps()
    {
        Assert.Empty(Schedule.FiveMinuteSteps(Start, 0));
    }
}
