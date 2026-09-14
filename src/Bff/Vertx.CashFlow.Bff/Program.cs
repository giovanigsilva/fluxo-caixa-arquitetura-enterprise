using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Vertx.CashFlow.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("proxy", client => client.Timeout = TimeSpan.FromSeconds(20));
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
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", auth = "password-totp-local" }));

var auth = app.MapGroup("/bff/login").WithTags("Login approval");

auth.MapPost("/start", async (FileCashFlowStore store, LoginStartRequest request, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["credentials"] = ["Informe login e senha."]
        });
    }

    var credentials = LoginCredentials.FromEnvironment();
    if (credentials is null)
    {
        return Results.Problem("Credencial local do BFF não configurada.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!credentials.Verify(request.Email, request.Password))
    {
        return Results.Problem("Login ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);
    }

    var secret = Base64Url(RandomNumberGenerator.GetBytes(32));
    var matchCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString(System.Globalization.CultureInfo.InvariantCulture);
    var totpSecret = Totp.CreateSecret();
    var challengeId = $"lac_{Guid.NewGuid():N}";
    var sessionId = $"ps_{Guid.NewGuid():N}";
    var expiresAt = DateTimeOffset.UtcNow.AddSeconds(120);
    var totpUri = Totp.CreateOtpAuthUri(credentials.Issuer, credentials.Email, totpSecret);
    var displayName = "Administrador";

    var result = await store.MutateAsync(state =>
    {
        var user = state.Users.FirstOrDefault(item => item.Id == credentials.UserId && !item.Disabled);
        if (user is null)
        {
            return Results.Problem("Usuário local não encontrado ou desativado.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        displayName = user.DisplayName;
        state.LoginChallenges.Add(new LoginApprovalChallenge
        {
            Id = challengeId,
            UserId = credentials.UserId,
            PendingSessionId = sessionId,
            SecretHash = FileCashFlowStore.HashSecret(secret),
            MatchCode = matchCode,
            TotpSecretBase32 = totpSecret,
            ExpiresAt = expiresAt
        });
        return Results.Ok(new LoginStartResponse(
            challengeId,
            sessionId,
            matchCode,
            $"https://{credentials.ApprovalHost}/aprovar-login#{challengeId}.{secret}",
            totpUri,
            credentials.Issuer,
            displayName,
            expiresAt));
    }, ct);

    return result;
})
.WithSummary("Valida senha e cria challenge TOTP por QR Code compatível com Google Authenticator.");

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

        var legacyApproval = !string.IsNullOrWhiteSpace(request.UserId)
            && !string.IsNullOrWhiteSpace(request.Secret)
            && !string.IsNullOrWhiteSpace(request.MatchCode)
            && string.Equals(challenge.UserId, request.UserId, StringComparison.Ordinal)
            && string.Equals(challenge.MatchCode, request.MatchCode, StringComparison.Ordinal)
            && string.Equals(challenge.SecretHash, FileCashFlowStore.HashSecret(request.Secret), StringComparison.Ordinal);
        var totpApproval = !string.IsNullOrWhiteSpace(request.TotpCode)
            && !string.IsNullOrWhiteSpace(challenge.TotpSecretBase32)
            && Totp.Verify(challenge.TotpSecretBase32, request.TotpCode);

        if (!legacyApproval && !totpApproval)
        {
            challenge.DeniedAt = DateTimeOffset.UtcNow;
            return Results.Problem("Aprovação recusada.", statusCode: StatusCodes.Status403Forbidden);
        }

        challenge.ApprovedAt = DateTimeOffset.UtcNow;
        var user = state.Users.FirstOrDefault(item => item.Id == challenge.UserId);
        return Results.Ok(new LoginApproveResponse(
            "approved",
            challenge.PendingSessionId,
            challenge.UserId,
            user?.DisplayName ?? "Administrador",
            DateTimeOffset.UtcNow.AddHours(8)));
    }, ct);

    return approved;
})
.WithSummary("Aprova challenge com TOTP do QR Code ou com o contrato legado de segredo e código.");

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

internal sealed record LoginStartRequest(string? Email, string? Password);
internal sealed record LoginStartResponse(
    string ChallengeId,
    string PendingSessionId,
    string MatchCode,
    string ApprovalUrl,
    string TotpUri,
    string TotpIssuer,
    string DisplayName,
    DateTimeOffset ExpiresAt);
internal sealed record LoginApproveRequest(string ChallengeId, string? UserId, string? Secret, string? MatchCode, string? TotpCode);
internal sealed record LoginApproveResponse(string Status, string PendingSessionId, string UserId, string DisplayName, DateTimeOffset ExpiresAt);

internal sealed record LoginCredentials(string Email, string PasswordSha256, string UserId, string Issuer, string ApprovalHost)
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
            Environment.GetEnvironmentVariable("VERTX_LOGIN_USER_ID") ?? SeedFactory.AdminUserId,
            Environment.GetEnvironmentVariable("VERTX_LOGIN_TOTP_ISSUER") ?? "Vertx CashFlow",
            Environment.GetEnvironmentVariable("VERTX_LOGIN_APPROVAL_HOST") ?? "vertx.dwilon.com");
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

internal static class Totp
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int TimeStepSeconds = 30;

    public static string CreateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return ToBase32(bytes);
    }

    public static string CreateOtpAuthUri(string issuer, string accountName, string secret)
    {
        var label = $"{issuer}:{accountName}";
        return $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period={TimeStepSeconds}";
    }

    public static bool Verify(string secret, string code)
    {
        var normalized = new string(code.Where(char.IsDigit).ToArray());
        if (normalized.Length != 6)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            if (string.Equals(GenerateCode(secret, now + offset), normalized, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateCode(string secret, long counter)
    {
        var key = FromBase32(secret);
        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static string ToBase32(byte[] bytes)
    {
        var output = new StringBuilder((bytes.Length + 4) / 5 * 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return output.ToString();
    }

    private static byte[] FromBase32(string value)
    {
        var buffer = 0;
        var bitsLeft = 0;
        var bytes = new List<byte>();
        foreach (var character in value.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = Alphabet.IndexOf(character, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 255));
                bitsLeft -= 8;
            }
        }

        return bytes.ToArray();
    }
}
