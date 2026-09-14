using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vertx.CashFlow.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("proxy", client => client.Timeout = TimeSpan.FromSeconds(180));
builder.Services.AddHttpClient("recaptcha", client =>
{
    client.BaseAddress = new Uri("https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(FileCashFlowStore.FromEnvironment(builder.Environment.EnvironmentName));

var app = builder.Build();
app.MapGet("/openapi/v1.json", () => Results.Text(SwaggerDocumentation.OpenApiJson, "application/json; charset=utf-8"))
    .ExcludeFromDescription();
app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"))
    .ExcludeFromDescription();
app.MapGet("/swagger/index.html", () => Results.Text(SwaggerDocumentation.Html, "text/html; charset=utf-8"))
    .ExcludeFromDescription();
app.MapGet("/", () => Results.Ok(new { name = "Fluxo de Caixa", bff = "ready" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", auth = "password-recaptcha-v2" }));

var auth = app.MapGroup("/bff/login").WithTags("Login approval");

auth.MapGet("/recaptcha/config", () =>
{
    var recaptcha = RecaptchaConfig.FromEnvironment();
    return Results.Ok(new RecaptchaConfigResponse(
        "google-recaptcha-v2-checkbox",
        recaptcha is not null,
        recaptcha?.SiteKey));
})
.WithSummary("Retorna a configuração pública do reCAPTCHA v2.");

auth.MapPost("/start", async (FileCashFlowStore store, IHttpClientFactory factory, HttpContext context, LoginStartRequest request, CancellationToken ct) =>
{
    var validationErrors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        validationErrors["credentials"] = ["Informe login e senha."];
    }

    if (string.IsNullOrWhiteSpace(request.RecaptchaToken))
    {
        validationErrors["recaptchaToken"] = ["Confirme o reCAPTCHA."];
    }

    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var email = request.Email!;
    var password = request.Password!;
    var recaptchaToken = request.RecaptchaToken!;
    var recaptcha = RecaptchaConfig.FromEnvironment();
    if (recaptcha is null)
    {
        return Results.Problem("reCAPTCHA do BFF não configurado.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var verification = await RecaptchaVerifier.VerifyAsync(
        factory.CreateClient("recaptcha"),
        recaptcha,
        recaptchaToken,
        context.Connection.RemoteIpAddress?.ToString(),
        ct);
    if (!verification.Success)
    {
        return Results.Problem(
            verification.Unavailable ? "Serviço reCAPTCHA indisponível." : "Falha na validação reCAPTCHA.",
            statusCode: verification.Unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status403Forbidden);
    }

    var credentials = LoginCredentials.FromEnvironment();
    if (credentials is null)
    {
        return Results.Problem("Credencial local do BFF não configurada.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!credentials.Verify(email, password))
    {
        return Results.Problem("Login ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);
    }

    var sessionId = $"ps_{Guid.NewGuid():N}";
    var state = await store.ReadAsync(ct);
    var user = state.Users.FirstOrDefault(item => item.Id == credentials.UserId && !item.Disabled);
    if (user is null)
    {
        return Results.Problem("Usuário local não encontrado ou desativado.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new LoginSessionResponse(
        "approved",
        sessionId,
        credentials.UserId,
        user.DisplayName,
        DateTimeOffset.UtcNow.AddHours(8)));
})
.WithSummary("Valida senha e token Google reCAPTCHA v2 para liberar a sessão web.");

MapProxy(app, "/api/entries/{**path}", "ENTRIES_URL", "http://127.0.0.1:6222", "/api/v1/");
MapProxy(app, "/api/management/{**path}", "MANAGEMENT_URL", "http://127.0.0.1:6221", "/api/v1/");
MapProxy(app, "/api/consolidated/{**path}", "CONSOLIDATION_URL", "http://127.0.0.1:6223", "/api/v1/");
MapProxy(app, "/api/observability/{**path}", "OBSERVABILITY_URL", "http://127.0.0.1:6224", "/api/v1/");
MapProxy(app, "/api/agent/{**path}", "SUPPORT_AGENT_URL", "http://127.0.0.1:6225", "/api/v1/");

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

internal sealed record RecaptchaConfigResponse(string Provider, bool Enabled, string? SiteKey);
internal sealed record LoginStartRequest(string? Email, string? Password, string? RecaptchaToken);
internal sealed record LoginSessionResponse(string Status, string PendingSessionId, string UserId, string DisplayName, DateTimeOffset ExpiresAt);

internal sealed record LoginCredentials(string Email, string PasswordSha256, string UserId)
{
    public static LoginCredentials? FromEnvironment()
    {
        var email = Environment.GetEnvironmentVariable("VERTX_LOGIN_EMAIL");
        var passwordSha256 = NormalizeHash(Environment.GetEnvironmentVariable("VERTX_LOGIN_PASSWORD_SHA256"));
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(passwordSha256))
        {
            return null;
        }

        return new LoginCredentials(
            email.Trim(),
            passwordSha256,
            Environment.GetEnvironmentVariable("VERTX_LOGIN_USER_ID") ?? SeedFactory.AdminUserId);
    }

    public bool Verify(string email, string password)
    {
        if (!string.Equals(Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var expected = Convert.FromHexString(PasswordSha256);
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? value.Trim()[7..]
            : value.Trim();
    }
}

internal sealed record RecaptchaConfig(string SiteKey, string SecretKey, string[] AllowedHosts)
{
    public static RecaptchaConfig? FromEnvironment()
    {
        var siteKey = Environment.GetEnvironmentVariable("VERTX_RECAPTCHA_SITE_KEY");
        var secretKey = Environment.GetEnvironmentVariable("VERTX_RECAPTCHA_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(siteKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            return null;
        }

        var hosts = (Environment.GetEnvironmentVariable("VERTX_RECAPTCHA_ALLOWED_HOSTS")
                ?? "vertx.dwilon.com,uat.vertx.dwilon.com,localhost,127.0.0.1")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new RecaptchaConfig(siteKey.Trim(), secretKey.Trim(), hosts);
    }
}

internal sealed record RecaptchaVerificationResult(bool Success, bool Unavailable, string[] ErrorCodes);

internal static class RecaptchaVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<RecaptchaVerificationResult> VerifyAsync(
        HttpClient client,
        RecaptchaConfig config,
        string token,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["secret"] = config.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            form["remoteip"] = remoteIp;
        }

        try
        {
            using var response = await client.PostAsync("recaptcha/api/siteverify", new FormUrlEncodedContent(form), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new RecaptchaVerificationResult(false, true, ["recaptcha.http"]);
            }

            var payload = await JsonSerializer.DeserializeAsync<RecaptchaVerificationResponse>(
                    await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                    JsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (payload is null || !payload.Success)
            {
                return new RecaptchaVerificationResult(false, false, payload?.ErrorCodes ?? ["recaptcha.invalid"]);
            }

            if (!string.IsNullOrWhiteSpace(payload.Hostname)
                && !config.AllowedHosts.Contains(payload.Hostname, StringComparer.OrdinalIgnoreCase))
            {
                return new RecaptchaVerificationResult(false, false, ["recaptcha.hostname"]);
            }

            return new RecaptchaVerificationResult(true, false, []);
        }
        catch (HttpRequestException)
        {
            return new RecaptchaVerificationResult(false, true, ["recaptcha.unavailable"]);
        }
        catch (TaskCanceledException)
        {
            return new RecaptchaVerificationResult(false, true, ["recaptcha.timeout"]);
        }
    }
}

internal sealed record RecaptchaVerificationResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("challenge_ts")] DateTimeOffset? ChallengeTs,
    [property: JsonPropertyName("hostname")] string? Hostname,
    [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
