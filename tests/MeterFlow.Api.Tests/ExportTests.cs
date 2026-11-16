using System.Net;
using System.Net.Http.Json;
using DuckDB.NET.Data;
using MeterFlow.Domain;
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
