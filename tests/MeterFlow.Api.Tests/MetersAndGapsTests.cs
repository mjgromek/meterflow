using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace MeterFlow.Api.Tests;

public class MetersAndGapsTests(MeterFlowFactory factory) : IClassFixture<MeterFlowFactory>
{
    private record Meter(string Ean, int Readings, DateTimeOffset First, DateTimeOffset Last);

    private record Gaps(string Ean, int Count, List<DateTimeOffset> Moments);

    private static DateTimeOffset At(string time) => DateTimeOffset.Parse($"2026-03-02T{time}Z");

    [Fact]
    public async Task Meters_are_summarised_by_ean_and_gaps_are_counted_per_meter_with_readings_in_range()
    {
        string a = ReadingsTests.TestEan(30), b = ReadingsTests.TestEan(31), c = ReadingsTests.TestEan(32);
        string json = $$"""
            [
              {"ean":"{{c}}","moment":"2026-03-02T10:00:00Z","kwh":0.1},
              {"ean":"{{c}}","moment":"2026-03-02T12:00:00+01:00","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-03-02T10:00:00Z","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-03-02T10:05:00Z","kwh":0.1},
              {"ean":"{{b}}","moment":"2026-03-02T10:20:00Z","kwh":0.1},
              {"ean":"{{a}}","moment":"2026-03-02T10:00:00Z","kwh":0.1},
              {"ean":"{{a}}","moment":"2026-03-02T10:10:00Z","kwh":0.1},
              {"ean":"{{a}}","moment":"2026-03-02T11:00:00Z","kwh":0.1}
            ]
            """;
        var client = factory.CreateClient();
        await client.PostAsync("/readings", new StringContent(json, Encoding.UTF8, "application/json"));
        await ReadingsTests.WaitUntilStored(factory, a, 3);

        var meters = await client.GetFromJsonAsync<List<Meter>>("/meters");
        var gaps = await client.GetFromJsonAsync<List<Gaps>>(
            $"/gaps?from={Uri.EscapeDataString("2026-03-02T11:00:00+01:00")}&to={Uri.EscapeDataString("2026-03-02T11:00:00Z")}");

        Assert.Equal(
            [new(a, 3, At("10:00:00"), At("11:00:00")), new(b, 3, At("10:00:00"), At("10:20:00")), new(c, 2, At("10:00:00"), At("11:00:00"))],
            meters);
        Assert.NotNull(gaps);
        Assert.Equal([(b, 2), (a, 1), (c, 0)], gaps.Select(g => (g.Ean, g.Count)));
        Assert.Equal([At("10:10:00"), At("10:15:00")], gaps[0].Moments);
    }

    [Theory]
    [InlineData("2026-03-02T10:00:00", "2026-03-02T11:00:00Z")]
    [InlineData("2026-03-02T10:00:00Z", "garbage")]
    [InlineData("2026-03-02T11:00:00Z", "2026-03-02T10:00:00Z")]
    public async Task Gaps_returns_400_for_bad_or_offsetless_bounds_or_from_not_before_to(string from, string to)
    {
        var response = await factory.CreateClient().GetAsync($"/gaps?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
