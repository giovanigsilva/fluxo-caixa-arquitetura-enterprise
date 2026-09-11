using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SimulationEngine>();

var app = builder.Build();
app.MapOpenApi();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", mode = "synthetic-only" }));

var api = app.MapGroup("/api/v1").WithTags("Observability simulation");

api.MapGet("/scenarios", () => Results.Ok(SimulationEngine.Scenarios))
.WithSummary("Lista cenários sintéticos disponíveis.");

api.MapGet("/samples", (SimulationEngine engine, string environment = "uat", string scenario = "NORMAL") =>
{
    return Results.Ok(engine.Sample(environment, scenario));
})
.WithSummary("Obtém uma amostra sintética determinística de monitoramento.");

api.MapPost("/scenarios/{scenario}", (SimulationEngine engine, string scenario, ScenarioSelection selection) =>
{
    engine.Select(selection.Environment, scenario);
    return Results.Ok(engine.Sample(selection.Environment, scenario));
})
.WithSummary("Seleciona cenário sintético sem alterar variáveis de processo ou containers.");

api.MapGet("/stream", (SimulationEngine engine, HttpContext context, string environment = "uat", string scenario = "NORMAL", CancellationToken ct = default) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    return Stream(engine, environment, scenario, ct);
})
.WithSummary("Stream SSE autenticável de amostras sintéticas.");

app.Run();

static async IAsyncEnumerable<string> Stream(
    SimulationEngine engine,
    string environment,
    string scenario,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var sample = engine.Sample(environment, scenario);
        yield return $"data: {System.Text.Json.JsonSerializer.Serialize(sample)}\n\n";
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }
}

internal sealed record ScenarioSelection(string Environment);

internal sealed class SimulationEngine
{
    public static readonly string[] Scenarios =
    [
        "NORMAL",
        "LOAD_50",
        "LOAD_100",
        "SPIKE_200",
        "WORKER_DOWN",
        "BROKER_DOWN",
        "READ_DB_DOWN",
        "REDIS_DOWN",
        "DUPLICATE_EVENT",
        "RECOVERY"
    ];

    private readonly Dictionary<string, SyntheticState> states = new(StringComparer.OrdinalIgnoreCase);

    public void Select(string environment, string scenario)
    {
        _ = GetState(environment, scenario);
    }

    public SyntheticSample Sample(string environment, string scenario)
    {
        var state = GetState(environment, scenario);
        state.Tick++;
        var profile = ProfileFor(scenario);
        var entriesPerSecond = profile.EntriesPerSecond;
        var ackPerSecond = profile.AckPerSecond;
        var publishPerSecond = profile.PublishPerSecond;

        state.OutboxReady += entriesPerSecond;
        state.OutboxReady -= Math.Min(state.OutboxReady, publishPerSecond);
        state.BrokerReady += publishPerSecond;
        state.BrokerReady -= Math.Min(state.BrokerReady, ackPerSecond);
        state.Projected += ackPerSecond;

        if (scenario.Equals("WORKER_DOWN", StringComparison.OrdinalIgnoreCase))
        {
            state.BrokerReady += entriesPerSecond;
        }

        if (scenario.Equals("BROKER_DOWN", StringComparison.OrdinalIgnoreCase))
        {
            state.OutboxReady += entriesPerSecond;
        }

        if (scenario.Equals("RECOVERY", StringComparison.OrdinalIgnoreCase))
        {
            state.BrokerReady = Math.Max(0, state.BrokerReady - 78.125);
            state.OutboxReady = Math.Max(0, state.OutboxReady - 20);
        }

        var errorRate = profile.ErrorRate;
        var eligible = Math.Max(1, profile.ReadRps + profile.WriteRps);
        var failures = eligible * errorRate;
        var budget = eligible * 0.0005;
        double? remaining = budget <= 0 ? null : Math.Max(0, 1 - failures / budget);

        return new SyntheticSample(
            DataSource: "synthetic",
            Environment: environment,
            ScenarioId: scenario,
            Timestamp: DateTimeOffset.UtcNow,
            Window: "PT60S",
            Health: new
            {
                entriesApi = "planned-or-ready",
                consolidationApi = scenario.Equals("READ_DB_DOWN", StringComparison.OrdinalIgnoreCase) ? "degraded" : "planned-or-ready",
                rabbitMq = scenario.Equals("BROKER_DOWN", StringComparison.OrdinalIgnoreCase) ? "simulated-down" : "operational-simulated",
                redisCache = scenario.Equals("REDIS_DOWN", StringComparison.OrdinalIgnoreCase) ? "simulated-down" : "operational-simulated",
                observabilityStack = "prepared-disabled",
                aiAssistant = "future-disabled"
            },
            Metrics: new
            {
                readRps = profile.ReadRps,
                writeRps = profile.WriteRps,
                p50Ms = profile.P50,
                p95Ms = Math.Max(profile.P95, profile.P50),
                p99Ms = Math.Max(profile.P99, profile.P95),
                errors4xx = profile.Errors4xx,
                errors5xx = profile.Errors5xx,
                errors429 = profile.Errors429,
                outboxPending = (int)Math.Round(state.OutboxReady),
                rabbitReady = (int)Math.Round(state.BrokerReady),
                projectedEntries = (int)Math.Round(state.Projected),
                duplicatesIgnored = scenario.Equals("DUPLICATE_EVENT", StringComparison.OrdinalIgnoreCase) ? 37 + state.Tick : 0,
                errorBudgetRemaining = remaining,
                unit = "requests"
            });
    }

    private SyntheticState GetState(string environment, string scenario)
    {
        if (!Scenarios.Contains(scenario, StringComparer.OrdinalIgnoreCase))
        {
            scenario = "NORMAL";
        }

        var key = $"{environment}:{scenario}";
        if (!states.TryGetValue(key, out var state))
        {
            state = new SyntheticState();
            states[key] = state;
        }

        return state;
    }

    private static ScenarioProfile ProfileFor(string scenario)
        => scenario.ToUpperInvariant() switch
        {
            "LOAD_50" => new ScenarioProfile(50, 15, 12, 12, 15, 92, 180, 260, 0.00018, 1, 0, 0),
            "LOAD_100" => new ScenarioProfile(100, 30, 25, 25, 25, 110, 220, 360, 0.003, 3, 1, 0),
            "SPIKE_200" => new ScenarioProfile(200, 45, 35, 30, 20, 145, 360, 520, 0.018, 8, 2, 4),
            "WORKER_DOWN" => new ScenarioProfile(50, 25, 25, 25, 0, 95, 210, 330, 0.001, 1, 0, 0),
            "BROKER_DOWN" => new ScenarioProfile(50, 25, 25, 0, 0, 88, 190, 300, 0.001, 1, 0, 0),
            "READ_DB_DOWN" => new ScenarioProfile(8, 0, 0, 0, 0, 0, 0, 0, 0.75, 0, 45, 0),
            "REDIS_DOWN" => new ScenarioProfile(50, 15, 12, 12, 12, 120, 260, 390, 0.002, 2, 0, 0),
            "DUPLICATE_EVENT" => new ScenarioProfile(50, 25, 25, 25, 25, 90, 185, 280, 0.0002, 1, 0, 0),
            "RECOVERY" => new ScenarioProfile(50, 25, 25, 25, 103.125, 105, 230, 360, 0.0006, 2, 0, 0),
            _ => new ScenarioProfile(30, 10, 8, 8, 8, 82, 160, 240, 0.00018, 1, 0, 0)
        };
}

internal sealed class SyntheticState
{
    public int Tick { get; set; }
    public double OutboxReady { get; set; }
    public double BrokerReady { get; set; }
    public double Projected { get; set; }
}

internal sealed record ScenarioProfile(
    int ReadRps,
    int WriteRps,
    double EntriesPerSecond,
    double PublishPerSecond,
    double AckPerSecond,
    int P50,
    int P95,
    int P99,
    double ErrorRate,
    int Errors4xx,
    int Errors5xx,
    int Errors429);

internal sealed record SyntheticSample(
    string DataSource,
    string Environment,
    string ScenarioId,
    DateTimeOffset Timestamp,
    string Window,
    object Health,
    object Metrics);
