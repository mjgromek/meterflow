using MeterFlow.Domain;

namespace MeterFlow.Simulator;

public static class MeterSeries
{
    public static IEnumerable<Reading> For(Ean ean, DateTimeOffset start, int count, Random rng) =>
        Schedule.FiveMinuteSteps(start, count)
            .Select(moment => new Reading(ean, moment, MeterProfile.KwhAt(moment, rng)));
}
