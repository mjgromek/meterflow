using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterFlow.Api.Tests;

public class ConsumptionTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private record Quarter(DateTimeOffset Start, decimal Kwh);

    private Task<HttpResponseMessage> Get(string ean, string from, string to) =>
        factory.CreateClient().GetAsync(
            $"/meters/{ean}/consumption?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

    [Fact]
    public async Task Consumption_returns_quarters_in_from_inclusive_to_exclusive_ordered_by_start()
    {
        string ean = ReadingsTests.TestEan(20);
        string json = $$"""
            [
              {"ean":"{{ean}}","moment":"2026-03-02T10:30:00+01:00","kwh":0.8},
              {"ean":"{{ean}}","moment":"2026-03-02T10:15:00+01:00","kwh":0.4},
              {"ean":"{{ean}}","moment":"2026-03-02T10:05:00+01:00","kwh":0.2},
              {"ean":"{{ean}}","moment":"2026-03-02T10:00:00+01:00","kwh":0.1},
              {"ean":"{{ean}}","moment":"2026-03-02T09:55:00+01:00","kwh":1.6}
            ]
            """;
        var client = factory.CreateClient();
        await client.PostAsync("/readings", new StringContent(json, Encoding.UTF8, "application/json"));

        var response = await Get(ean, "2026-03-02T10:00:00+01:00", "2026-03-02T09:30:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quarters = await response.Content.ReadFromJsonAsync<List<Quarter>>();
        Assert.Equal(
            [new(DateTimeOffset.Parse("2026-03-02T09:00:00Z"), 0.3m), new(DateTimeOffset.Parse("2026-03-02T09:15:00Z"), 0.4m)],
            quarters);
    }

    [Fact]
    public async Task Consumption_of_an_unknown_meter_is_empty()
    {
        var response = await Get(ReadingsTests.TestEan(21), "2026-03-02T10:00:00+01:00", "2026-03-03T10:00:00+01:00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([], await response.Content.ReadFromJsonAsync<List<Quarter>>());
    }

    [Theory]
    [InlineData("123", "2026-03-02T10:00:00+01:00", "2026-03-02T11:00:00+01:00")]
    [InlineData(null, "2026-03-02T10:00:00", "2026-03-02T11:00:00+01:00")]
    [InlineData(null, "2026-03-02T10:00:00+01:00", "garbage")]
    [InlineData(null, "2026-03-02T10:00:00+01:00", "")]
    [InlineData(null, "2026-03-02T10:00:00+01:00", "2026-03-02T09:00:00Z")]
    [InlineData(null, "2026-03-02T11:00:00+01:00", "2026-03-02T10:00:00+01:00")]
    public async Task Consumption_returns_400_for_an_invalid_ean_bad_or_offsetless_bounds_or_from_not_before_to(
        string? ean, string from, string to)
    {
        var response = await Get(ean ?? ReadingsTests.TestEan(22), from, to);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
