namespace MeterFlow.Simulator;

public static class Schedule
{
    public static IEnumerable<DateTimeOffset> FiveMinuteSteps(DateTimeOffset start, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return start.AddMinutes(i * 5);
        }
    }
}
