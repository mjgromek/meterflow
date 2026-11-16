using System.Globalization;
using System.Text.Json.Serialization;
using MeterFlow.Domain;
using Parquet.Serialization;
using Parquet.Serialization.Attributes;

namespace MeterFlow.Infrastructure;

/// <summary>Writes one Europe/Amsterdam day of quarter-hour totals as Parquet under a Hive-style date path.</summary>
public static class QuarterExport
{
    private static readonly TimeZoneInfo Amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    // One export at a time across all days: exports are rare, and a per-day lock table is not worth its cleanup.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private record Row(
        [property: JsonPropertyName("ean")] string Ean,
        [property: JsonPropertyName("start"), ParquetTimestamp(ParquetTimestampResolution.Microseconds, useLogicalTimestamp: true, isAdjustedToUTC: true)] DateTime Start,
        [property: JsonPropertyName("kwh")] decimal Kwh);

    public static async Task<(string Path, int Rows)> WriteAsync(IReadingStore store, DateOnly day, string root, CancellationToken cancel = default)
    {
        var inv = CultureInfo.InvariantCulture;
        string directory = Path.Combine(root, day.ToString("'year='yyyy", inv), day.ToString("'month='MM", inv), day.ToString("'day='dd", inv));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "quarters.parquet");
        string temp = $"{path}.tmp-{Guid.NewGuid():N}";
        // Read inside the lock, so the export that writes last also read last.
        await Gate.WaitAsync(cancel);
        try
        {
            var starts = QuarterHours.ForLocalDay(day, Amsterdam).ToList();
            var rows = QuarterHours.Aggregate(store.Between(starts[0], starts[^1] + QuarterHours.Length))
                .OrderBy(q => q.Ean.Value, StringComparer.Ordinal)
                .ThenBy(q => q.Start)
                .Select(q => new Row(q.Ean.Value, q.Start.UtcDateTime, q.Kwh))
                .ToList();
            await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
            {
                await ParquetSerializer.SerializeAsync(rows, file, cancellationToken: cancel);
            }
            File.Move(temp, path, overwrite: true);
            return (path, rows.Count);
        }
        finally
        {
            File.Delete(temp);
            Gate.Release();
        }
    }
}
