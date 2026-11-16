using MeterFlow.Domain;

namespace MeterFlow.Simulator.Tests;

/// <summary>Simulator output through CsvExport, ReadingCsv.Parse, ReadingChecks and QuarterHours on the 25-hour DST day.</summary>
public class RoundTripTests
{
    private static readonly TimeZoneInfo Amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
    private static readonly DateOnly Day = new(2026, 10, 25);
    private static readonly Ean[] Meters = [SyntheticEan.For(1), SyntheticEan.For(2)];

    private static List<Reading> TwoMeterDay(int seed)
    {
        var start = QuarterHours.ForLocalDay(Day, Amsterdam).First();
        int count = QuarterHours.ForLocalDay(Day, Amsterdam).Count() * 3;
        var rng = new Random(seed);
        return Meters.SelectMany(ean => MeterSeries.For(ean, start, count, rng).ToList()).ToList();
    }

    private static ReadingCsvResult RoundTrip(IEnumerable<Reading> readings)
    {
        var writer = new StringWriter();
        CsvExport.Write(writer, readings);
        return ReadingCsv.Parse(writer.ToString().Split(writer.NewLine, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Csv_round_trip_rejects_nothing_and_returns_the_input()
    {
        var input = TwoMeterDay(seed: 1);

        var result = RoundTrip(input);

        Assert.Empty(result.Rejected);
        Assert.Equal(600, input.Count);
        Assert.Equal(input, result.Readings);
    }

    [Fact]
    public void Checks_report_exactly_the_duplicates_and_gaps_faults_injected()
    {
        var input = TwoMeterDay(seed: 2);
        Assert.Empty(ReadingChecks.FindDuplicates(RoundTrip(input).Readings));
        Assert.Empty(ReadingChecks.FindGaps(RoundTrip(input).Readings));

        var faulted = Faults.Inject(input, new Random(3), missingChance: 0.05, duplicateChance: 0.05).ToList();
        var parsed = RoundTrip(faulted).Readings;

        var expectedDuplicates = faulted.GroupBy(r => (r.Ean, r.Moment)).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var bounds = faulted.GroupBy(r => r.Ean).ToDictionary(g => g.Key, g => (First: g.Min(r => r.Moment), Last: g.Max(r => r.Moment)));
        var present = faulted.Select(r => (r.Ean, r.Moment)).ToHashSet();
        // Gaps exist only between two readings, so drops before a meter's first or after its last are not gaps.
        var expectedGaps = input
            .Select(r => (r.Ean, r.Moment))
            .Where(k => !present.Contains(k) && k.Moment > bounds[k.Ean].First && k.Moment < bounds[k.Ean].Last)
            .ToHashSet();

        Assert.NotEmpty(expectedDuplicates);
        Assert.NotEmpty(expectedGaps);
        Assert.Equal(expectedDuplicates, ReadingChecks.FindDuplicates(parsed).ToHashSet());
        Assert.Equal(expectedGaps, ReadingChecks.FindGaps(parsed).ToHashSet());
    }

    [Fact]
    public void Each_meter_gets_the_days_100_quarters_and_keeps_its_own_total()
    {
        var input = TwoMeterDay(seed: 4);

        var quarters = QuarterHours.Aggregate(RoundTrip(input).Readings).ToList();

        var expectedStarts = QuarterHours.ForLocalDay(Day, Amsterdam).ToList();
        Assert.Equal(100, expectedStarts.Count);
        foreach (var ean in Meters)
        {
            var own = quarters.Where(q => q.Ean == ean).ToList();
            Assert.Equal(expectedStarts, own.Select(q => q.Start).Order());
            Assert.Equal(input.Where(r => r.Ean == ean).Sum(r => r.Kwh), own.Sum(q => q.Kwh));
        }
    }
}
