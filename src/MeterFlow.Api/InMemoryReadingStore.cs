using System.Collections.Concurrent;
using MeterFlow.Domain;

namespace MeterFlow.Api;

public class InMemoryReadingStore : IReadingStore
{
    // DateTimeOffset equality compares instants, so the same moment in two offsets is one key.
    private readonly ConcurrentDictionary<(Ean, DateTimeOffset), Reading> readings = new();

    public bool Add(Reading reading) => readings.TryAdd((reading.Ean, reading.Moment), reading);

    // shortcut: scans every stored reading, replaced by an indexed query with persistence in M5.
    public IEnumerable<Reading> ForMeter(Ean ean, DateTimeOffset from, DateTimeOffset to) =>
        readings.Values
            .Where(r => r.Ean == ean && r.Moment >= from && r.Moment < to)
            .OrderBy(r => r.Moment);
}
