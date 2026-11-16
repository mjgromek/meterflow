using MeterFlow.Domain;

namespace MeterFlow.Simulator;

/// <summary>Drops and duplicates readings at random, the way real meter feeds do.</summary>
public static class Faults
{
    public static IEnumerable<Reading> Inject(
        IEnumerable<Reading> readings, Random rng, double missingChance, double duplicateChance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(missingChance);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(missingChance, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(duplicateChance);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(duplicateChance, 1);

        return InjectIterator(readings, rng, missingChance, duplicateChance);
    }

    private static IEnumerable<Reading> InjectIterator(
        IEnumerable<Reading> readings, Random rng, double missingChance, double duplicateChance)
    {
        foreach (var reading in readings)
        {
            if (rng.NextDouble() < missingChance)
            {
                continue;
            }

            yield return reading;

            if (rng.NextDouble() < duplicateChance)
            {
                yield return reading;
            }
        }
    }
}
