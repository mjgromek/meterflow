using System.Net;
using System.Net.Http.Json;
using System.Text;
using MeterFlow.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MeterFlow.Api.Tests;

public class ReadingsTests(MeterFlowFactory factory) : IClassFixture<MeterFlowFactory>
{
    private record Rejection(int Index, string Reason);

    private record PostResult(int Accepted, List<Rejection> Rejected);

    // The factory, and so the store, is shared by the class: each test uses its own meter.
    internal static string TestEan(int index)
    {
        string body = $"999{index:D14}";
        for (int digit = 0; digit <= 9; digit++)
        {
            if (Ean.TryParse($"{body}{digit}", out var ean))
            {
                return ean.Value;
            }
        }
        throw new InvalidOperationException();
    }

    private Task<HttpResponseMessage> Post(string json) => Post(factory, json);

    private static Task<HttpResponseMessage> Post(WebApplicationFactory<Program> api, string json) =>
        api.CreateClient().PostAsync("/readings", new StringContent(json, Encoding.UTF8, "application/json"));

    private static List<Reading> Stored(WebApplicationFactory<Program> api, string ean)
    {
        using var scope = api.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IReadingStore>()
            .ForMeter(Ean.Parse(ean), DateTimeOffset.MinValue, DateTimeOffset.MaxValue).ToList();
    }

    [Fact]
    public async Task Post_stores_valid_items_and_rejects_the_rest_by_index()
    {
        string ean = TestEan(1);
        string json = $$"""
            [
              {"ean":"{{ean}}","moment":"2026-03-02T10:00:00+01:00","kwh":0.25},
              {"ean":"123","moment":"2026-03-02T10:05:00+01:00","kwh":0.25},
              {"ean":"{{ean}}","moment":"2026-03-02T10:05:00+01:00","kwh":-1},
              {"ean":"{{ean}}","moment":"2026-03-02T09:00:00+00:00","kwh":0.5},
              {"ean":"{{ean}}","moment":"2026-03-02T10:15:00+01:00","kwh":0}
            ]
            """;

        var response = await Post(json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PostResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Accepted);
        Assert.Equal([new(1, "InvalidEan"), new(2, "NegativeKwh"), new(3, "Duplicate")], result.Rejected);

        Assert.Equal([0.25m, 0m], Stored(factory, ean).Select(r => r.Kwh));
    }

    [Fact]
    public async Task Post_rejects_a_reading_already_stored_as_duplicate()
    {
        string json = $$"""[{"ean":"{{TestEan(2)}}","moment":"2026-03-02T10:00:00+01:00","kwh":0.25}]""";
        await Post(json);

        var result = await (await Post(json)).Content.ReadFromJsonAsync<PostResult>();

        Assert.NotNull(result);
        Assert.Equal(0, result.Accepted);
        Assert.Equal([new(0, "Duplicate")], result.Rejected);
    }

    [Fact]
    public async Task Post_rejects_items_with_missing_fields_or_null_as_bad_format()
    {
        string ean = TestEan(6);
        string json = $$"""
            [
              {"ean":"{{ean}}","moment":"2026-03-02T10:00:00+01:00","kwh":0.25},
              {"ean":"{{ean}}","kwh":0.25},
              {"ean":"{{ean}}","moment":"2026-03-02T10:05:00+01:00"},
              null,
              {"moment":"2026-03-02T10:10:00+01:00","kwh":0.25}
            ]
            """;

        var result = await (await Post(json)).Content.ReadFromJsonAsync<PostResult>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Accepted);
        Assert.Equal([new(1, "BadFormat"), new(2, "BadFormat"), new(3, "BadFormat"), new(4, "BadFormat")], result.Rejected);
        Assert.Single(Stored(factory, ean));
    }

    [Theory]
    [InlineData("""[{"ean":""")]
    [InlineData("""{"ean":"999000000000000001","moment":"2026-03-02T10:00:00+01:00","kwh":1}""")]
    [InlineData("""[{"ean":"999000000000000001","moment":"2026-03-02T10:00:00+01:00","kwh":"1.5"}]""")]
    public async Task Post_returns_400_for_malformed_json_a_non_array_body_or_a_string_kwh(string json)
    {
        var response = await Post(json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(10, "2026-03-02T10:00:00+01:00", 1)]
    [InlineData(11, "2026-03-02T09:00:00Z", 1)]
    [InlineData(12, "2026-03-02T10:00:00.0000000+01:00", 1)]
    [InlineData(13, "2026-03-02T10:00:00", 0)]
    [InlineData(14, "2026-03-02", 0)]
    public async Task Post_accepts_a_moment_only_with_an_explicit_offset(int meter, string moment, int accepted)
    {
        string json = $$"""[{"ean":"{{TestEan(meter)}}","moment":"{{moment}}","kwh":1}]""";

        var result = await (await Post(json)).Content.ReadFromJsonAsync<PostResult>();

        Assert.NotNull(result);
        Assert.Equal(accepted, result.Accepted);
        Assert.Equal(accepted == 1 ? [] : [new Rejection(0, "BadFormat")], result.Rejected);
    }

    [Fact]
    public async Task Concurrent_identical_posts_store_the_reading_once()
    {
        string json = $$"""[{"ean":"{{TestEan(3)}}","moment":"2026-03-02T10:00:00+01:00","kwh":0.25}]""";

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Post(json)));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PostResult>()));

        Assert.Equal(1, results.Sum(r => r!.Accepted));
    }

    [Fact]
    public async Task Readings_survive_a_restart_on_the_same_database_file()
    {
        string ean = TestEan(4);
        string path = Path.Combine(Path.GetTempPath(), $"meterflow-{Guid.NewGuid():N}.db");
        try
        {
            using (var first = new MeterFlowFactory(path))
            {
                await Post(first, $$"""[{"ean":"{{ean}}","moment":"2026-03-02T10:00:00+01:00","kwh":0.25}]""");
            }

            using var second = new MeterFlowFactory(path);
            Assert.Equal([0.25m], Stored(second, ean).Select(r => r.Kwh));
        }
        finally
        {
            MeterFlowFactory.DeleteDatabase(path);
        }
    }
}
