using Vertx.CashFlow.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(FileCashFlowStore.FromEnvironment(builder.Environment.EnvironmentName));

var app = builder.Build();
app.MapOpenApi();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (FileCashFlowStore store, CancellationToken ct) =>
{
    var state = await store.ReadAsync(ct);
    return Results.Ok(new
    {
        status = "ready",
        readModel = "daily-balances",
        projectedEntries = state.ProjectedEntryIds.Count,
        pendingOutbox = state.Outbox.Count(item => item.ProjectedAt is null)
    });
});

var api = app.MapGroup("/api/v1").WithTags("Consolidation");

api.MapGet("/daily", async (HttpContext http, FileCashFlowStore store, DateOnly? from, DateOnly? to, string? accountId, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    var query = state.DailyBalances.Where(item => item.TenantId == tenantId);
    if (from is not null)
    {
        query = query.Where(item => item.BusinessDate >= from);
    }

    if (to is not null)
    {
        query = query.Where(item => item.BusinessDate <= to);
    }

    if (!string.IsNullOrWhiteSpace(accountId))
    {
        query = query.Where(item => item.AccountId == accountId);
    }

    var rows = query
        .OrderBy(item => item.BusinessDate)
        .ThenBy(item => item.AccountId)
        .ToArray();
    return Results.Ok(new
    {
        tenantId,
        generatedAt = DateTimeOffset.UtcNow,
        dataSource = "read-model",
        lag = new
        {
            outboxPending = state.Outbox.Count(item => item.TenantId == tenantId && item.ProjectedAt is null),
            oldestPendingOccurredAt = state.Outbox.Where(item => item.TenantId == tenantId && item.ProjectedAt is null).OrderBy(item => item.OccurredAt).Select(item => (DateTimeOffset?)item.OccurredAt).FirstOrDefault()
        },
        rows
    });
})
.WithSummary("Consulta consolidado diário projetado, com indicação de defasagem.");

api.MapPost("/rebuild", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var result = await store.MutateAsync(state =>
    {
        state.DailyBalances.RemoveAll(item => item.TenantId == tenantId);
        state.ProjectedEntryIds.RemoveAll(item => state.Entries.Any(entry => entry.TenantId == tenantId && entry.Id == item));
        foreach (var outbox in state.Outbox.Where(item => item.TenantId == tenantId))
        {
            outbox.ProjectedAt = null;
        }

        return new { tenantId, status = "accepted", generation = Guid.NewGuid().ToString("N") };
    }, ct);
    return Results.Accepted("/api/v1/daily", result);
})
.WithSummary("Solicita reconstrução controlada da projeção do tenant.");

app.Run();

static string Tenant(HttpContext http)
    => http.Request.Headers.TryGetValue("X-Tenant-Id", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString()
        : SeedFactory.AlphaTenantId;
