using System.Net.Http.Json;
using MeterFlow.Domain;

namespace MeterFlow.Simulator;

/// <summary>Posts readings to the API's POST /readings in batches and totals what it accepted and rejected.</summary>
public static class ApiSender
{
    private record Item(string Ean, DateTimeOffset Moment, decimal Kwh);

    private record Rejection(int Index, string Reason);

    private record Result(int Accepted, List<Rejection> Rejected);

    public static async Task<(int Accepted, int Rejected)> SendAsync(HttpClient client, IEnumerable<Reading> readings, int batchSize)
    {
        int accepted = 0, rejected = 0;
        foreach (var batch in readings.Chunk(batchSize))
        {
            // DateTimeOffset serializes as ISO 8601 with its offset, which the API requires.
            var response = await client.PostAsJsonAsync("/readings", batch.Select(r => new Item(r.Ean.Value, r.Moment, r.Kwh)));
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result>()
                ?? throw new InvalidOperationException("POST /readings returned an empty body.");
            accepted += result.Accepted;
            rejected += result.Rejected.Count;
        }
        return (accepted, rejected);
    }
}
