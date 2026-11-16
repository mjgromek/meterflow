using System.Text.Json;
using MeterFlow.Domain;
using MeterFlow.Mcp;
using MeterFlow.Simulator;

namespace MeterFlow.Api.Tests;

/// <summary>The MCP tools against the in-process API: which meter had the most gaps on the 25-hour DST day.</summary>
public class McpToolsTests(MeterFlowFactory factory) : IClassFixture<MeterFlowFactory>
{
    private const string From = "2026-10-25T00:00:00+02:00", To = "2026-10-26T00:00:00+01:00";

    [Fact]
    public async Task Find_gaps_puts_the_meter_with_the_most_missing_readings_first_and_list_meters_shows_both()
    {
        var start = DateTimeOffset.Parse(From);
        var rng = new Random(8);
        Ean[] meters = [SyntheticEan.For(1), SyntheticEan.For(2)];
        double[] missingChance = [0.02, 0.08];
        var sent = meters
            .SelectMany((ean, i) => Faults.Inject(MeterSeries.For(ean, start, 300, rng), rng, missingChance[i], 0.01))
            .ToList();
        // Every moment of the 300-moment window without a reading is a gap, edges included.
        var expected = meters
            .Select(ean =>
            {
                var kept = sent.Where(r => r.Ean == ean).Select(r => r.Moment).ToHashSet();
                var missing = Enumerable.Range(0, 300).Select(i => start + i * ReadingChecks.Interval)
                    .Count(m => !kept.Contains(m));
                return (Ean: ean.Value, Count: missing);
            })
            .OrderByDescending(m => m.Count)
            .ToList();
        var client = factory.CreateClient();
        await ApiSender.SendAsync(client, sent, batchSize: 1000);
        await ReadingsTests.WaitUntilStored(factory, meters[1].Value, sent.Where(r => r.Ean == meters[1]).DistinctBy(r => r.Moment).Count());
        var tools = new MeterFlowApi(client);

        using var gaps = JsonDocument.Parse(await MeterFlowTools.FindGaps(tools, From, To, CancellationToken.None));
        using var listed = JsonDocument.Parse(await MeterFlowTools.ListMeters(tools, CancellationToken.None));

        Assert.True(expected[0].Count > expected[1].Count);
        Assert.Equal(
            expected,
            gaps.RootElement.EnumerateArray().Select(g => (g.GetProperty("ean").GetString()!, g.GetProperty("count").GetInt32())));
        Assert.Equal(meters.Select(m => m.Value), listed.RootElement.EnumerateArray().Select(m => m.GetProperty("ean").GetString()));
    }

    private sealed class Status500 : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
    }

    [Fact]
    public async Task Tools_return_an_error_text_naming_the_api_when_it_is_unreachable()
    {
        // Nothing listens on port 1.
        var tools = new MeterFlowApi(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") });

        string meters = await MeterFlowTools.ListMeters(tools, CancellationToken.None);

        Assert.StartsWith("Error:", meters);
        Assert.Contains("http://127.0.0.1:1/", meters);
    }

    private sealed class Slow : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancel);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Tools_return_an_error_text_naming_the_api_when_it_times_out()
    {
        var http = new HttpClient(new Slow()) { BaseAddress = new Uri("http://meterflow.test"), Timeout = TimeSpan.FromMilliseconds(50) };

        string meters = await MeterFlowTools.ListMeters(new MeterFlowApi(http), CancellationToken.None);

        Assert.StartsWith("Error:", meters);
        Assert.Contains("http://meterflow.test/", meters);
        Assert.Contains("did not respond in time", meters);
    }

    [Fact]
    public async Task Tools_return_an_error_text_naming_the_api_on_a_server_error()
    {
        var tools = new MeterFlowApi(new HttpClient(new Status500()) { BaseAddress = new Uri("http://meterflow.test") });

        string meters = await MeterFlowTools.ListMeters(tools, CancellationToken.None);

        Assert.StartsWith("Error:", meters);
        Assert.Contains("http://meterflow.test/", meters);
        Assert.Contains("500", meters);
    }

    [Fact]
    public async Task Tools_return_an_error_text_instead_of_throwing_when_the_api_rejects_the_bounds()
    {
        var tools = new MeterFlowApi(factory.CreateClient());

        string gaps = await MeterFlowTools.FindGaps(tools, "2026-10-25T00:00:00", To, CancellationToken.None);
        string consumption = await MeterFlowTools.GetConsumption(tools, SyntheticEan.For(1).Value, To, From, CancellationToken.None);

        Assert.StartsWith("Error:", gaps);
        Assert.StartsWith("Error:", consumption);
    }
}
