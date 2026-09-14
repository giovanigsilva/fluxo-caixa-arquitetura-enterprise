using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vertx.CashFlow.BuildingBlocks;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public readonly record struct Money(decimal Value)
{
    public static Money ParsePositive(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new CashFlowValidationException("amount.invalid", "Valor monetário inválido.");
        }

        if (decimal.Round(parsed, 2) != parsed || parsed <= 0)
        {
            throw new CashFlowValidationException("amount.scale", "O valor deve ser positivo e ter duas casas decimais.");
        }

        return new Money(parsed);
    }

    public string ToCanonicalString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
}

public sealed class CashFlowValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class PermissionCatalog
{
    public static readonly string[] Version1 =
    [
        "dashboard.view",
        "customers.read",
        "customers.create",
        "customers.update",
        "customers.delete",
        "customers.read-sensitive",
        "accounts.read",
        "accounts.create",
        "accounts.update",
        "accounts.delete",
        "categories.read",
        "categories.create",
        "categories.update",
        "categories.delete",
        "entries.read",
        "entries.create-draft",
        "entries.update-draft",
        "entries.delete-draft",
        "entries.post",
        "entries.reverse",
        "consolidated.read",
        "statements.read",
        "statements.export-pdf",
        "statements.export-xlsx",
        "statements.export-csv",
        "users.read",
        "users.invite",
        "users.update",
        "users.disable",
        "roles.read",
        "roles.manage",
        "permissions.assign",
        "audit.read",
        "observability.view",
        "observability.scenarios.manage",
        "settings.read",
        "settings.manage"
    ];
}

public sealed class CashFlowState
{
    public int SchemaVersion { get; set; } = 1;
    public List<CustomerRecord> Customers { get; set; } = [];
    public List<CashAccountRecord> Accounts { get; set; } = [];
    public List<CategoryRecord> Categories { get; set; } = [];
    public List<CostCenterRecord> CostCenters { get; set; } = [];
    public List<UserRecord> Users { get; set; } = [];
    public List<RoleRecord> Roles { get; set; } = [];
    public List<EntryRecord> Entries { get; set; } = [];
    public List<OutboxRecord> Outbox { get; set; } = [];
    public List<string> ProjectedEntryIds { get; set; } = [];
    public List<DailyBalanceRecord> DailyBalances { get; set; } = [];
    public List<IdempotencyRecord> Idempotency { get; set; } = [];
    public List<StatementExportJob> StatementExports { get; set; } = [];
    public List<AuditRecord> Audit { get; set; } = [];
    public List<LoginApprovalChallenge> LoginChallenges { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public abstract class TenantEntity
{
    public required string Id { get; set; }
    public required string TenantId { get; set; }
    public long Version { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomerRecord : TenantEntity
{
    public required string LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool RestrictedData { get; set; }
}

public sealed class CashAccountRecord : TenantEntity
{
    public required string Name { get; set; }
    public required string Currency { get; set; }
    public string? Description { get; set; }
}

public sealed class CategoryRecord : TenantEntity
{
    public required string Name { get; set; }
    public required string Type { get; set; }
}

public sealed class CostCenterRecord : TenantEntity
{
    public required string Name { get; set; }
}

public sealed class UserRecord : TenantEntity
{
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public List<string> RoleIds { get; set; } = [];
    public bool Disabled { get; set; }
}

public sealed class RoleRecord : TenantEntity
{
    public required string Name { get; set; }
    public List<string> Permissions { get; set; } = [];
    public bool Protected { get; set; }
}

public sealed class EntryRecord : TenantEntity
{
    public required string AccountId { get; set; }
    public required string Type { get; set; }
    public required string Amount { get; set; }
    public required DateOnly BusinessDate { get; set; }
    public required string Description { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string? CustomerId { get; set; }
    public string? CategoryId { get; set; }
    public string? CostCenterId { get; set; }
    public string? ReversesEntryId { get; set; }
    public string? ReversalEntryId { get; set; }
    public string? Reason { get; set; }
    public required string ActorUserId { get; set; }
}

public sealed class OutboxRecord
{
    public required string EventId { get; set; }
    public required string TenantId { get; set; }
    public required string EntryId { get; set; }
    public required string EventType { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ProjectedAt { get; set; }
}

public sealed class DailyBalanceRecord
{
    public required string TenantId { get; set; }
    public required string AccountId { get; set; }
    public required string Currency { get; set; }
    public required DateOnly BusinessDate { get; set; }
    public decimal Credits { get; set; }
    public decimal Debits { get; set; }
    public int EntryCount { get; set; }
    public decimal DayMovement => Credits - Debits;
}

public sealed class IdempotencyRecord
{
    public required string TenantId { get; set; }
    public required string Operation { get; set; }
    public required string Key { get; set; }
    public required string PayloadHash { get; set; }
    public required string ResultJson { get; set; }
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StatementExportJob
{
    public required string Id { get; set; }
    public required string TenantId { get; set; }
    public required string Format { get; set; }
    public required string RequestedBy { get; set; }
    public string Status { get; set; } = "Pending";
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string? AccountId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? FailureCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
}

public sealed class AuditRecord
{
    public required string Id { get; set; }
    public required string TenantId { get; set; }
    public required string ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string ResourceId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LoginApprovalChallenge
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string PendingSessionId { get; set; }
    public required string SecretHash { get; set; }
    public required string MatchCode { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? DeniedAt { get; set; }
}

public sealed class FileCashFlowStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string path;

    public FileCashFlowStore(string path)
    {
        this.path = path;
    }

    public static FileCashFlowStore FromEnvironment(string environmentName)
    {
        var configured = Environment.GetEnvironmentVariable("VERTX_STATE_FILE");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return new FileCashFlowStore(configured);
        }

        var root = Environment.GetEnvironmentVariable("VERTX_RUNTIME_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, ".runtime", environmentName.ToLowerInvariant());
        return new FileCashFlowStore(Path.Combine(root, "cashflow-state.json"));
    }

    public async Task<CashFlowState> ReadAsync(CancellationToken cancellationToken)
    {
        var gate = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<TResult> MutateAsync<TResult>(
        Func<CashFlowState, TResult> mutation,
        CancellationToken cancellationToken)
    {
        var gate = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var result = mutation(state);
            state.UpdatedAt = DateTimeOffset.UtcNow;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + ".tmp";
            await using (var stream = File.Create(temp))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken).ConfigureAwait(false);
            }

            File.Move(temp, path, overwrite: true);
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<CashFlowState> ReadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return SeedFactory.CreateUatState();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<CashFlowState>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? new CashFlowState();
    }

    public static string ComputeHash<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    public static string HashSecret(string secret)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
}

public static class SeedFactory
{
    public const string AlphaTenantId = "org-alpha";
    public const string BetaTenantId = "org-beta";
    public const string AdminUserId = "user-admin-alpha";

    public static CashFlowState CreateUatState()
    {
        var state = new CashFlowState();
        AddTenant(state, AlphaTenantId, "Financeiro Alfa");
        AddTenant(state, BetaTenantId, "Operação Beta");
        return state;
    }

    private static void AddTenant(CashFlowState state, string tenantId, string suffix)
    {
        var account = new CashAccountRecord
        {
            Id = $"acc-{tenantId}-main",
            TenantId = tenantId,
            Name = $"Caixa principal {suffix}",
            Currency = "BRL",
            Description = "Conta de caixa seed UAT"
        };
        state.Accounts.Add(account);
        state.Categories.Add(new CategoryRecord
        {
            Id = $"cat-{tenantId}-sales",
            TenantId = tenantId,
            Name = "Receitas operacionais",
            Type = "Credit"
        });
        state.Categories.Add(new CategoryRecord
        {
            Id = $"cat-{tenantId}-ops",
            TenantId = tenantId,
            Name = "Despesas operacionais",
            Type = "Debit"
        });
        state.CostCenters.Add(new CostCenterRecord
        {
            Id = $"cc-{tenantId}-default",
            TenantId = tenantId,
            Name = "Administrativo"
        });
        state.Customers.Add(new CustomerRecord
        {
            Id = $"cus-{tenantId}-demo",
            TenantId = tenantId,
            LegalName = $"Cliente fictício {suffix}",
            TradeName = $"Cliente {suffix}",
            Email = "cliente.ficticio@example.invalid",
            Phone = "+55 11 90000-0000"
        });
        state.Roles.Add(new RoleRecord
        {
            Id = $"role-{tenantId}-admin",
            TenantId = tenantId,
            Name = "TenantAdmin",
            Protected = true,
            Permissions = PermissionCatalog.Version1.ToList()
        });
        state.Users.Add(new UserRecord
        {
            Id = tenantId == AlphaTenantId ? AdminUserId : $"user-admin-{tenantId}",
            TenantId = tenantId,
            Username = $"bootstrap-admin-{tenantId}",
            DisplayName = $"Administrador {suffix}",
            RoleIds = [$"role-{tenantId}-admin"]
        });
    }
}

public static class TenantEntityExtensions
{
    public static T RequireTenant<T>(this IEnumerable<T> source, string tenantId, string id, string resource)
        where T : TenantEntity
    {
        var entity = source.FirstOrDefault(item => item.TenantId == tenantId && item.Id == id && !item.IsDeleted);
        return entity ?? throw new CashFlowValidationException($"{resource}.not_found", "Recurso inexistente para o tenant informado.");
    }
}
