using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Vertx.CashFlow.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("proxy", client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(FileCashFlowStore.FromEnvironment(builder.Environment.EnvironmentName));

var app = builder.Build();
app.MapOpenApi();
app.MapGet("/", () => Results.Ok(new { name = "Fluxo de Caixa", bff = "ready" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", auth = "local-approval-partial" }));

var auth = app.MapGroup("/bff/login").WithTags("Login approval");

auth.MapPost("/start", async (FileCashFlowStore store, LoginStartRequest request, CancellationToken ct) =>
{
    var secret = Base64Url(RandomNumberGenerator.GetBytes(32));
    var matchCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString(System.Globalization.CultureInfo.InvariantCulture);
    var challengeId = $"lac_{Guid.NewGuid():N}";
    var sessionId = $"ps_{Guid.NewGuid():N}";

    await store.MutateAsync(state =>
    {
        state.LoginChallenges.Add(new LoginApprovalChallenge
        {
            Id = challengeId,
            UserId = request.UserId,
            PendingSessionId = sessionId,
            SecretHash = FileCashFlowStore.HashSecret(secret),
            MatchCode = matchCode,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(120)
        });
        return true;
    }, ct);

    return Results.Ok(new LoginStartResponse(
        challengeId,
        sessionId,
        matchCode,
        $"https://uat.vertx.dwilon.com/aprovar-login#{challengeId}.{secret}",
        DateTimeOffset.UtcNow.AddSeconds(120)));
})
.WithSummary("Cria challenge first-party de aprovação por segundo dispositivo.");

auth.MapGet("/status/{challengeId}", async (FileCashFlowStore store, string challengeId, CancellationToken ct) =>
{
    var state = await store.ReadAsync(ct);
    var challenge = state.LoginChallenges.FirstOrDefault(item => item.Id == challengeId);
    if (challenge is null)
    {
        return Results.NotFound();
    }

    var status = challenge.ApprovedAt is not null
        ? "approved"
        : challenge.DeniedAt is not null
            ? "denied"
            : challenge.ExpiresAt <= DateTimeOffset.UtcNow ? "expired" : "pending";
    return Results.Ok(new { challengeId, status, challenge.ExpiresAt });
})
.WithSummary("Consulta aprovação da sessão desktop pendente.");

auth.MapPost("/approve", async (FileCashFlowStore store, LoginApproveRequest request, CancellationToken ct) =>
{
    var approved = await store.MutateAsync(state =>
    {
        var challenge = state.LoginChallenges.FirstOrDefault(item => item.Id == request.ChallengeId);
        if (challenge is null)
        {
            return Results.NotFound();
        }

        if (challenge.ApprovedAt is not null || challenge.DeniedAt is not null || challenge.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Results.Problem("Challenge expirado ou já consumido.", statusCode: StatusCodes.Status409Conflict);
        }

        if (!string.Equals(challenge.UserId, request.UserId, StringComparison.Ordinal)
            || !string.Equals(challenge.MatchCode, request.MatchCode, StringComparison.Ordinal)
            || !string.Equals(challenge.SecretHash, FileCashFlowStore.HashSecret(request.Secret), StringComparison.Ordinal))
        {
            challenge.DeniedAt = DateTimeOffset.UtcNow;
            return Results.Problem("Aprovação recusada.", statusCode: StatusCodes.Status403Forbidden);
        }

        challenge.ApprovedAt = DateTimeOffset.UtcNow;
        return Results.Ok(new { status = "approved", challenge.PendingSessionId });
    }, ct);

    return approved;
})
.WithSummary("Consome challenge por POST autenticável, com segredo e código de correspondência.");

MapProxy(app, "/api/entries/{**path}", "ENTRIES_URL", "http://127.0.0.1:6222", "/api/v1/");
MapProxy(app, "/api/management/{**path}", "MANAGEMENT_URL", "http://127.0.0.1:6221", "/api/v1/");
MapProxy(app, "/api/consolidated/{**path}", "CONSOLIDATION_URL", "http://127.0.0.1:6223", "/api/v1/");
MapProxy(app, "/api/observability/{**path}", "OBSERVABILITY_URL", "http://127.0.0.1:6224", "/api/v1/");

app.Run();

static void MapProxy(WebApplication app, string pattern, string envName, string defaultBaseUrl, string prefix)
{
    app.MapMethods(pattern, ["GET", "POST", "PUT", "DELETE"], async (
        HttpContext context,
        IHttpClientFactory factory,
        string? path,
        CancellationToken ct) =>
    {
        var baseUrl = Environment.GetEnvironmentVariable(envName) ?? defaultBaseUrl;
        var target = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), prefix.Trim('/') + "/" + (path ?? string.Empty) + context.Request.QueryString);
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
        foreach (var header in context.Request.Headers)
        {
            if (!WebHeaderCollection.IsRestricted(header.Key) && !header.Key.StartsWith("Host", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (context.Request.ContentLength > 0)
        {
            request.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            {
                request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
            }
        }

        var response = await factory.CreateClient("proxy").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, ct);
    })
    .WithTags("Proxy")
    .WithSummary($"Proxy fixo para {envName}.");
}

static string Base64Url(byte[] bytes)
    => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

internal sealed record LoginStartRequest(string UserId);
internal sealed record LoginStartResponse(string ChallengeId, string PendingSessionId, string MatchCode, string ApprovalUrl, DateTimeOffset ExpiresAt);
internal sealed record LoginApproveRequest(string ChallengeId, string UserId, string Secret, string MatchCode);
