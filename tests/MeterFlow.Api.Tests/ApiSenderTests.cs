using MeterFlow.Simulator;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterFlow.Api.Tests;

public class ApiSenderTests(MeterFlowFactory factory) : IClassFixture<MeterFlowFactory>
{
    private sealed class CountingHandler : DelegatingHandler
    {
        public int Requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Requests);
            return base.SendAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task SendAsync_posts_in_batches_and_totals_accepted_and_rejected()
    {
        var start = new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.FromHours(1));
        var readings = MeterSeries.For(SyntheticEan.For(30), start, 4, new Random(1)).ToList();
        readings.Insert(2, readings[1]);
        var counter = new CountingHandler();

        var totals = await ApiSender.SendAsync(factory.CreateDefaultClient(counter), readings, batchSize: 2);

        Assert.Equal((4, 1), totals);
        Assert.Equal(3, counter.Requests);
    }
}
