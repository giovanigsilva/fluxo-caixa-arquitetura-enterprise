using System.Globalization;
using Vertx.CashFlow.BuildingBlocks;

namespace Vertx.CashFlow.ConsolidationWorker;

public sealed class Worker(FileCashFlowStore store, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var projected = await store.MutateAsync(state =>
            {
                var now = DateTimeOffset.UtcNow;
                var batch = state.Outbox
                    .Where(item => item.PublishedAt is not null && item.ProjectedAt is null)
                    .OrderBy(item => item.OccurredAt)
                    .Take(100)
                    .ToArray();
                var count = 0;
                foreach (var item in batch)
                {
                    if (state.ProjectedEntryIds.Contains(item.EntryId, StringComparer.Ordinal))
                    {
                        item.ProjectedAt = now;
                        continue;
                    }

                    var entry = state.Entries.FirstOrDefault(candidate => candidate.TenantId == item.TenantId && candidate.Id == item.EntryId);
                    if (entry is null)
                    {
                        continue;
                    }

                    var account = state.Accounts.FirstOrDefault(candidate => candidate.TenantId == entry.TenantId && candidate.Id == entry.AccountId);
                    if (account is null)
                    {
                        continue;
                    }

                    var amount = decimal.Parse(entry.Amount, CultureInfo.InvariantCulture);
                    var balance = state.DailyBalances.FirstOrDefault(candidate =>
                        candidate.TenantId == entry.TenantId
                        && candidate.AccountId == entry.AccountId
                        && candidate.Currency == account.Currency
                        && candidate.BusinessDate == entry.BusinessDate);
                    if (balance is null)
                    {
                        balance = new DailyBalanceRecord
                        {
                            TenantId = entry.TenantId,
                            AccountId = entry.AccountId,
                            Currency = account.Currency,
                            BusinessDate = entry.BusinessDate
                        };
                        state.DailyBalances.Add(balance);
                    }

                    if (entry.Type == "Credit")
                    {
                        balance.Credits += amount;
                    }
                    else
                    {
                        balance.Debits += amount;
                    }

                    balance.EntryCount++;
                    state.ProjectedEntryIds.Add(entry.Id);
                    item.ProjectedAt = now;
                    count++;
                }

                return count;
            }, stoppingToken);

            if (projected > 0)
            {
                logger.LogInformation("Projected {Count} entries into daily balances.", projected);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
