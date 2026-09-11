using Vertx.CashFlow.BuildingBlocks;

namespace Vertx.CashFlow.OutboxRelay;

public sealed class Worker(FileCashFlowStore store, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = await store.MutateAsync(state =>
            {
                var now = DateTimeOffset.UtcNow;
                var batch = state.Outbox
                    .Where(item => item.PublishedAt is null)
                    .OrderBy(item => item.OccurredAt)
                    .Take(50)
                    .ToArray();
                foreach (var item in batch)
                {
                    item.PublishedAt = now;
                }

                return batch.Length;
            }, stoppingToken);

            if (published > 0)
            {
                logger.LogInformation("Published {Count} outbox events to the local relay boundary.", published);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
