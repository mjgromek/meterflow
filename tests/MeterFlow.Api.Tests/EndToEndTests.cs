using System.Collections.Concurrent;
using System.Net.Http.Json;
using MeterFlow.Domain;
using MeterFlow.Simulator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace MeterFlow.Api.Tests;

/// <summary>Simulator output with faults sent to the in-process API, read back per meter on the 25-hour DST day.</summary>
public class EndToEndTests
{
    private record Quarter(DateTimeOffset Start, decimal Kwh);

    private sealed class LogCapture : ILoggerProvider, ILogger
    {
        public readonly ConcurrentQueue<string> Messages = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Enqueue(formatter(state, exception));

        public void Dispose()
        {
        }
    }

    [Fact]
    public async Task Each_meter_gets_100_quarters_totalling_its_accepted_readings_and_the_post_is_logged()
    {
        var amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var day = new DateOnly(2026, 10, 25);
        var starts = QuarterHours.ForLocalDay(day, amsterdam).ToList();
        Ean[] meters = [SyntheticEan.For(1), SyntheticEan.For(2)];
        var rng = new Random(5);
        var faulted = meters
            .SelectMany(ean => Faults.Inject(MeterSeries.For(ean, starts[0], starts.Count * 3, rng), rng, 0.01, 0.01))
            .ToList();
        var stored = faulted.DistinctBy(r => (r.Ean, r.Moment)).ToList();
        var log = new LogCapture();
        using var database = new MeterFlowFactory();
        using var factory = database.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(log)));
        var client = factory.CreateClient();

        var (accepted, rejected) = await ApiSender.SendAsync(client, faulted, batchSize: 1000);

        Assert.Equal(stored.Count, accepted);
        Assert.Equal(faulted.Count - stored.Count, rejected);
        Assert.NotEqual(0, rejected);
        Assert.Contains($"POST /readings: {accepted} accepted, {rejected} rejected", log.Messages);
        string from = Uri.EscapeDataString(starts[0].ToString("O"));
        string to = Uri.EscapeDataString(starts[^1].Add(QuarterHours.Length).ToString("O"));
        foreach (var ean in meters)
        {
            var quarters = await client.GetFromJsonAsync<List<Quarter>>($"/meters/{ean}/consumption?from={from}&to={to}");
            Assert.NotNull(quarters);
            Assert.Equal(starts, quarters.Select(q => q.Start));
            Assert.Equal(stored.Where(r => r.Ean == ean).Sum(r => r.Kwh), quarters.Sum(q => q.Kwh));
        }
    }
}
