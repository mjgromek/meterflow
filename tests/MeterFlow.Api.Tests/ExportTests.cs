using System.Net;
using System.Net.Http.Json;
using DuckDB.NET.Data;
using MeterFlow.Domain;
using MeterFlow.Infrastructure;
using MeterFlow.Simulator;
using Microsoft.AspNetCore.Hosting;

namespace MeterFlow.Api.Tests;

/// <summary>POST /exports/{date} writes the day's quarters as Parquet, read back with DuckDB.</summary>
public class ExportTests
{
    private record Quarter(DateTimeOffset Start, decimal Kwh);

    private record ExportResult(string Path, int Rows);

    [Fact]
    public async Task Exported_day_has_100_quarters_per_meter_and_duckdb_sums_match_the_api()
    {
        var amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var starts = QuarterHours.ForLocalDay(new DateOnly(2026, 10, 25), amsterdam).ToList();
        Ean[] meters = [SyntheticEan.For(1), SyntheticEan.For(2)];
        var rng = new Random(7);
        var readings = meters.SelectMany(ean => MeterSeries.For(ean, starts[0], starts.Count * 3, rng)).ToList();
        string root = Path.Combine(Path.GetTempPath(), $"meterflow-exports-{Guid.NewGuid():N}");
        try
        {
            using var database = new MeterFlowFactory();
            using var factory = database.WithWebHostBuilder(b => b.UseSetting("Export:Root", root));
            var client = factory.CreateClient();
            await ApiSender.SendAsync(client, readings, batchSize: 1000);
            await ReadingsTests.WaitUntilStored(factory, meters[1].Value, starts.Count * 3);

            await client.PostAsync("/exports/2026-10-25", null);
            var response = await client.PostAsync("/exports/2026-10-25", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExportResult>();
            Assert.NotNull(result);
            Assert.Equal(Path.Combine(root, "year=2026", "month=10", "day=25", "quarters.parquet"), result.Path);
            Assert.Equal(200, result.Rows);
            Assert.Single(Directory.GetFiles(root, "*", SearchOption.AllDirectories));

            using var duck = new DuckDBConnection("DataSource=:memory:");
            duck.Open();
            using var command = duck.CreateCommand();
            command.CommandText = $"SELECT ean, count(*), sum(kwh), any_value(typeof(start)), any_value(typeof(kwh)) FROM read_parquet('{result.Path}') GROUP BY ean ORDER BY ean";
            using var reader = command.ExecuteReader();
            string from = Uri.EscapeDataString(starts[0].ToString("O"));
            string to = Uri.EscapeDataString(starts[^1].Add(QuarterHours.Length).ToString("O"));
            foreach (var ean in meters.OrderBy(m => m.Value))
            {
                Assert.True(reader.Read());
                var quarters = await client.GetFromJsonAsync<List<Quarter>>($"/meters/{ean}/consumption?from={from}&to={to}");
                Assert.NotNull(quarters);
                Assert.Equal(ean.Value, reader.GetString(0));
                Assert.Equal(100L, reader.GetInt64(1));
                Assert.Equal(quarters.Sum(q => q.Kwh), reader.GetDecimal(2));
                Assert.StartsWith("TIMESTAMP", reader.GetString(3));
                Assert.StartsWith("DECIMAL", reader.GetString(4));
            }
            Assert.False(reader.Read());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class FixedStore(IReadOnlyList<Reading> readings, Action? onRead = null) : IReadingStore
    {
        public IEnumerable<Reading> Between(DateTimeOffset from, DateTimeOffset to)
        {
            onRead?.Invoke();
            return readings.Where(r => r.Moment >= from && r.Moment < to);
        }

        public bool Add(Reading reading) => throw new NotSupportedException();
        public IEnumerable<Reading> ForMeter(Ean ean, DateTimeOffset from, DateTimeOffset to) => throw new NotSupportedException();
        public IEnumerable<MeterSummary> Meters() => throw new NotSupportedException();
        public void AddRejections(IEnumerable<Rejection> rejections) => throw new NotSupportedException();
        public IEnumerable<Rejection> RejectionsBetween(DateTimeOffset from, DateTimeOffset to) => throw new NotSupportedException();
    }

    private static FixedStore StoreWithOneDay() =>
        new([.. MeterSeries.For(SyntheticEan.For(1), new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.FromHours(1)), 288, new Random(3))]);

    [Fact]
    public async Task Concurrent_exports_of_the_same_day_all_succeed_and_leave_only_the_parquet_file()
    {
        string root = Path.Combine(Path.GetTempPath(), $"meterflow-exports-{Guid.NewGuid():N}");
        try
        {
            var store = StoreWithOneDay();

            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                Task.Run(() => QuarterExport.WriteAsync(store, new DateOnly(2026, 3, 2), root))));

            Assert.All(results, r => Assert.Equal(96, r.Rows));
            Assert.Equal(["quarters.parquet"], Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task An_export_that_read_first_cannot_overwrite_a_later_export_with_older_rows()
    {
        string root = Path.Combine(Path.GetTempPath(), $"meterflow-exports-{Guid.NewGuid():N}");
        try
        {
            var day = new DateOnly(2026, 3, 2);
            var start = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.FromHours(1));
            using var reading = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var older = new FixedStore([.. MeterSeries.For(SyntheticEan.For(1), start, 288, new Random(3))], () => { reading.Set(); release.Wait(); });
            var newer = new FixedStore([.. new[] { SyntheticEan.For(1), SyntheticEan.For(2) }.SelectMany(ean => MeterSeries.For(ean, start, 288, new Random(4)))]);

            var first = Task.Run(() => QuarterExport.WriteAsync(older, day, root));
            reading.Wait();
            var second = Task.Run(() => QuarterExport.WriteAsync(newer, day, root));
            // The second export must wait for the first; give it time to (wrongly) finish before releasing the first.
            await Task.WhenAny(second, Task.Delay(500));
            release.Set();
            var results = await Task.WhenAll(first, second);

            using var duck = new DuckDBConnection("DataSource=:memory:");
            duck.Open();
            using var command = duck.CreateCommand();
            command.CommandText = $"SELECT count(*) FROM read_parquet('{results[1].Path}')";
            Assert.Equal(192L, command.ExecuteScalar());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task A_failed_export_keeps_the_previous_file_and_leaves_no_temp_file()
    {
        string root = Path.Combine(Path.GetTempPath(), $"meterflow-exports-{Guid.NewGuid():N}");
        try
        {
            var (path, _) = await QuarterExport.WriteAsync(StoreWithOneDay(), new DateOnly(2026, 3, 2), root);
            byte[] before = await File.ReadAllBytesAsync(path);
            // Too large for the Parquet decimal column, so serialization fails after the file is opened.
            var unwritable = new FixedStore([new(SyntheticEan.For(1), new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero), decimal.MaxValue)]);

            await Assert.ThrowsAnyAsync<Exception>(() => QuarterExport.WriteAsync(unwritable, new DateOnly(2026, 3, 2), root));

            Assert.Equal(before, await File.ReadAllBytesAsync(path));
            Assert.Equal([path], Directory.GetFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("25-10-2026")]
    public async Task Bad_date_is_400(string date)
    {
        using var factory = new MeterFlowFactory();

        var response = await factory.CreateClient().PostAsync($"/exports/{date}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
