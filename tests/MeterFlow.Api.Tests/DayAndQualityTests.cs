using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MeterFlow.Api.Tests;

public class DayAndQualityTests(MeterFlowFactory factory) : IClassFixture<MeterFlowFactory>
{
    private record DayQuarter(DateTimeOffset Start, decimal? Kwh, int Readings);

    private record DayRejection(DateTimeOffset? Moment, string Reason);

    private record MeterDay(List<DayQuarter> Quarters, List<DateTimeOffset> Gaps, List<DayRejection> Rejected);

    private record MeterQuality(string Ean, int Gaps, int Rejected, int Duplicates);

    private static DateTimeOffset Utc(string text) => DateTimeOffset.Parse(text + "Z");

    private Task<HttpResponseMessage> Post(string json) =>
        factory.CreateClient().PostAsync("/readings", new StringContent(json, Encoding.UTF8, "application/json"));

    [Fact]
    public async Task Day_has_every_local_quarter_with_counts_gaps_and_the_meters_stored_rejections()
    {
        string ean = ReadingsTests.TestEan(50);
        await Post($$"""
            [
              {"ean":"{{ean}}","moment":"2026-10-24T22:00:00Z","kwh":0.1},
              {"ean":"{{ean}}","moment":"2026-10-24T22:05:00Z","kwh":0.2},
              {"ean":"{{ean}}","moment":"2026-10-24T22:10:00Z","kwh":0.3},
              {"ean":"{{ean}}","moment":"2026-10-24T22:20:00Z","kwh":-1},
              {"ean":"{{ean}}","moment":"2026-10-24T22:25:00Z"},
              {"ean":"{{ean}}","moment":"2026-10-24T22:05:00Z","kwh":0.9},
              {"ean":"123","moment":"2026-10-24T22:15:00Z","kwh":0.1},
              {"ean":"{{ean}}","moment":"2026-10-24T22:30:00Z","kwh":0.4}
            ]
            """);
        await ReadingsTests.WaitUntilStored(factory, ean, 4);

        var day = await factory.CreateClient().GetFromJsonAsync<MeterDay>($"/meters/{ean}/days/2026-10-25");

        Assert.NotNull(day);
        Assert.Equal(100, day.Quarters.Count);
        Assert.Equal(
            [new(Utc("2026-10-24T22:00:00"), 0.6m, 3), new(Utc("2026-10-24T22:15:00"), null, 0), new(Utc("2026-10-24T22:30:00"), 0.4m, 1)],
            day.Quarters.Take(3));
        Assert.Equal([Utc("2026-10-24T22:15:00"), Utc("2026-10-24T22:20:00"), Utc("2026-10-24T22:25:00")], day.Gaps);
        Assert.Equal(
            [new(Utc("2026-10-24T22:05:00"), "Duplicate"), new(Utc("2026-10-24T22:20:00"), "NegativeKwh"), new(Utc("2026-10-24T22:25:00"), "BadFormat")],
            day.Rejected);
    }

    [Theory]
    [InlineData("123", "2026-10-25")]
    [InlineData("123456789012345675", "2026-13-01")]
    public async Task Day_returns_400_for_a_bad_ean_or_date(string ean, string date)
    {
        var response = await factory.CreateClient().GetAsync($"/meters/{ean}/days/{date}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Quality_counts_issues_per_meter_for_the_local_day_most_first()
    {
        string a = ReadingsTests.TestEan(51), b = ReadingsTests.TestEan(52), c = ReadingsTests.TestEan(54);
        await Post($$"""
            [
              {"ean":"{{a}}","moment":"2026-10-31T23:00:00Z","kwh":0.1},
              {"ean":"{{a}}","moment":"2026-10-31T23:20:00Z","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-10-31T23:00:00Z","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-10-31T23:00:00Z","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-10-31T23:05:00Z","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-10-31T23:05:00Z","kwh":-1},
              {"ean":"{{c}}","moment":"2026-10-31T23:00:00Z","kwh":0.1}
            ]
            """);
        await ReadingsTests.WaitUntilStored(factory, c, 1);

        var quality = await factory.CreateClient().GetFromJsonAsync<List<MeterQuality>>("/quality/2026-11-01");

        Assert.Equal([new(a, 3, 0, 0), new(b, 0, 1, 1), new(c, 0, 0, 0)], quality);
    }

    [Fact]
    public async Task OpenApi_document_has_schemas_for_the_response_records()
    {
        using var document = JsonDocument.Parse(await factory.CreateClient().GetStringAsync("/openapi/v1.json"));

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.All(
            ["MeterDay", "DayQuarter", "DayRejection", "MeterQuality", "Quarter", "MeterRow", "PostReadingsResult"],
            name => Assert.True(schemas.TryGetProperty(name, out _), name));
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://evil.example", false)]
    public async Task Cors_preflight_allows_only_the_configured_frontend_origin(string origin, bool allowed)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/meters");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(allowed, response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) && values.Single() == origin);
    }
}
