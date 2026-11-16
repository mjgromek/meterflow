using System.Threading.Channels;
using MeterFlow.Domain;

/// <summary>Stores queued readings in batches; on stop, drains the queue until the host's shutdown timeout.</summary>
sealed class ReadingProcessor(ChannelReader<Reading> queue, IServiceScopeFactory scopes, ILogger<ReadingProcessor> logger)
    : BackgroundService
{
    private CancellationToken shutdownTimeout;

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        shutdownTimeout = cancellationToken;
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await queue.WaitToReadAsync(stoppingToken))
            {
                StoreBatch();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        // The server stops before this service, so nothing new is queued while draining.
        while (queue.Count > 0)
        {
            if (shutdownTimeout.IsCancellationRequested)
            {
                logger.LogWarning("Shutdown timeout: {Dropped} queued readings dropped", queue.Count);
                return;
            }
            StoreBatch();
        }
        logger.LogInformation("Queue drained on shutdown");
    }

    private void StoreBatch()
    {
        List<Reading> batch = [];
        while (batch.Count < 1000 && queue.TryRead(out var reading))
        {
            batch.Add(reading);
        }
        try
        {
            using var scope = scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IReadingStore>();
            int stored = batch.Count(store.Add);
            logger.LogInformation("Stored {Stored} readings, {Duplicates} duplicates", stored, batch.Count - stored);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Readings stored before the failure stay stored; the rest of the batch is lost.
            logger.LogError(e, "Storing a batch of {Count} readings failed", batch.Count);
        }
    }
}
