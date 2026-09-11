using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Vertx.CashFlow.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(FileCashFlowStore.FromEnvironment(builder.Environment.EnvironmentName));

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = feature?.Error;
        var status = exception is CashFlowValidationException ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == 422 ? "Falha de validação" : "Erro interno",
            Detail = exception is CashFlowValidationException validationException ? validationException.Message : "A operação não pôde ser concluída.",
            Extensions = { ["code"] = exception is CashFlowValidationException codeException ? codeException.Code : "internal.error" }
        });
    });
});

app.MapOpenApi();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", store = "file-backed" }));

var api = app.MapGroup("/api/v1").WithTags("Entries");

api.MapGet("/customers", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Customers.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderBy(item => item.LegalName));
})
.WithSummary("Lista clientes do tenant com filtros básicos.");

api.MapPost("/customers", async (HttpContext http, FileCashFlowStore store, UpsertCustomerRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var created = await store.MutateAsync(state =>
    {
        var customer = new CustomerRecord
        {
            Id = NewId("cus"),
            TenantId = tenantId,
            LegalName = RequireText(request.LegalName, "customers.legal_name"),
            TradeName = request.TradeName,
            Email = request.Email,
            Phone = request.Phone,
            RestrictedData = request.RestrictedData
        };
        state.Customers.Add(customer);
        Audit(state, tenantId, actor, "customers.create", customer.Id);
        return customer;
    }, ct);
    return Results.Created($"/api/v1/customers/{created.Id}", created);
})
.WithSummary("Cria cliente/contraparte comercial.");

api.MapPut("/customers/{id}", async (HttpContext http, FileCashFlowStore store, string id, UpsertCustomerRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var expected = ExpectedVersion(http);
    var updated = await store.MutateAsync(state =>
    {
        var customer = state.Customers.RequireTenant(tenantId, id, "customers");
        EnsureVersion(customer, expected);
        customer.LegalName = RequireText(request.LegalName, "customers.legal_name");
        customer.TradeName = request.TradeName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.RestrictedData = request.RestrictedData;
        Touch(customer);
        Audit(state, tenantId, actor, "customers.update", customer.Id);
        return customer;
    }, ct);
    return Results.Ok(updated);
})
.WithSummary("Atualiza cliente com controle otimista por If-Match.");

api.MapDelete("/customers/{id}", async (HttpContext http, FileCashFlowStore store, string id, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var expected = ExpectedVersion(http);
    await store.MutateAsync(state =>
    {
        var customer = state.Customers.RequireTenant(tenantId, id, "customers");
        EnsureVersion(customer, expected);
        customer.IsDeleted = true;
        Touch(customer);
        Audit(state, tenantId, actor, "customers.delete", customer.Id);
        return true;
    }, ct);
    return Results.NoContent();
})
.WithSummary("Exclui logicamente cliente sem apagar histórico financeiro.");

api.MapGet("/accounts", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Accounts.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderBy(item => item.Name));
})
.WithSummary("Lista contas de caixa.");

api.MapPost("/accounts", async (HttpContext http, FileCashFlowStore store, UpsertAccountRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var created = await store.MutateAsync(state =>
    {
        var account = new CashAccountRecord
        {
            Id = NewId("acc"),
            TenantId = tenantId,
            Name = RequireText(request.Name, "accounts.name"),
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency.Trim().ToUpperInvariant(),
            Description = request.Description
        };
        state.Accounts.Add(account);
        Audit(state, tenantId, actor, "accounts.create", account.Id);
        return account;
    }, ct);
    return Results.Created($"/api/v1/accounts/{created.Id}", created);
})
.WithSummary("Cria conta de caixa.");

api.MapGet("/categories", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Categories.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderBy(item => item.Name));
})
.WithSummary("Lista categorias.");

api.MapPost("/categories", async (HttpContext http, FileCashFlowStore store, UpsertCategoryRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var created = await store.MutateAsync(state =>
    {
        var category = new CategoryRecord
        {
            Id = NewId("cat"),
            TenantId = tenantId,
            Name = RequireText(request.Name, "categories.name"),
            Type = NormalizeEntryType(request.Type)
        };
        state.Categories.Add(category);
        Audit(state, tenantId, actor, "categories.create", category.Id);
        return category;
    }, ct);
    return Results.Created($"/api/v1/categories/{created.Id}", created);
})
.WithSummary("Cria categoria financeira.");

api.MapGet("/cost-centers", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.CostCenters.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderBy(item => item.Name));
})
.WithSummary("Lista centros de custo simples.");

api.MapPost("/cost-centers", async (HttpContext http, FileCashFlowStore store, UpsertCostCenterRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var created = await store.MutateAsync(state =>
    {
        var costCenter = new CostCenterRecord
        {
            Id = NewId("cc"),
            TenantId = tenantId,
            Name = RequireText(request.Name, "cost_centers.name")
        };
        state.CostCenters.Add(costCenter);
        Audit(state, tenantId, actor, "cost_centers.create", costCenter.Id);
        return costCenter;
    }, ct);
    return Results.Created($"/api/v1/cost-centers/{created.Id}", created);
})
.WithSummary("Cria centro de custo.");

api.MapGet("/entries", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Entries.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderByDescending(item => item.BusinessDate).ThenByDescending(item => item.CreatedAt));
})
.WithSummary("Lista lançamentos confirmados e estornos do tenant.");

api.MapPost("/entries", async (HttpContext http, FileCashFlowStore store, PostEntryRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var key = IdempotencyKey(http);
    var hash = FileCashFlowStore.ComputeHash(request);

    var result = await store.MutateAsync(state =>
    {
        var replay = FindReplay(state, tenantId, "entries.post", key, hash);
        if (replay is not null)
        {
            return replay;
        }

        var account = state.Accounts.RequireTenant(tenantId, request.AccountId, "accounts");
        if (request.CustomerId is not null)
        {
            state.Customers.RequireTenant(tenantId, request.CustomerId, "customers");
        }

        if (request.CategoryId is not null)
        {
            state.Categories.RequireTenant(tenantId, request.CategoryId, "categories");
        }

        if (request.CostCenterId is not null)
        {
            state.CostCenters.RequireTenant(tenantId, request.CostCenterId, "cost_centers");
        }

        var amount = Money.ParsePositive(request.Amount);
        var entry = new EntryRecord
        {
            Id = NewId("ent"),
            TenantId = tenantId,
            AccountId = account.Id,
            Type = NormalizeEntryType(request.Type),
            Amount = amount.ToCanonicalString(),
            BusinessDate = request.BusinessDate,
            Description = RequireText(request.Description, "entries.description"),
            CustomerId = request.CustomerId,
            CategoryId = request.CategoryId,
            CostCenterId = request.CostCenterId,
            ActorUserId = actor
        };
        state.Entries.Add(entry);
        state.Outbox.Add(new OutboxRecord
        {
            EventId = NewId("evt"),
            TenantId = tenantId,
            EntryId = entry.Id,
            EventType = "cashflow.entry.confirmed.v1"
        });
        Audit(state, tenantId, actor, "entries.post", entry.Id);
        var response = JsonSerializer.Serialize(entry);
        state.Idempotency.Add(new IdempotencyRecord
        {
            TenantId = tenantId,
            Operation = "entries.post",
            Key = key,
            PayloadHash = hash,
            ResultJson = response,
            StatusCode = StatusCodes.Status201Created
        });
        return new IdempotencyOutcome(false, StatusCodes.Status201Created, response);
    }, ct);

    var entry = JsonSerializer.Deserialize<EntryRecord>(result.ResultJson)!;
    return result.Replayed ? Results.Ok(entry) : Results.Created($"/api/v1/entries/{entry.Id}", entry);
})
.WithSummary("Confirma lançamento financeiro imutável com idempotência.");

api.MapPost("/entries/{id}/reverse", async (HttpContext http, FileCashFlowStore store, string id, ReverseEntryRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var key = IdempotencyKey(http);
    var hash = FileCashFlowStore.ComputeHash(request);

    var result = await store.MutateAsync(state =>
    {
        var replay = FindReplay(state, tenantId, $"entries.reverse:{id}", key, hash);
        if (replay is not null)
        {
            return replay;
        }

        var original = state.Entries.RequireTenant(tenantId, id, "entries");
        if (original.ReversalEntryId is not null)
        {
            throw new CashFlowValidationException("entries.reversal.duplicate", "Este lançamento já possui estorno.");
        }

        var reversal = new EntryRecord
        {
            Id = NewId("ent"),
            TenantId = tenantId,
            AccountId = original.AccountId,
            Type = original.Type == "Credit" ? "Debit" : "Credit",
            Amount = original.Amount,
            BusinessDate = request.BusinessDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Description = $"Estorno: {original.Description}",
            CustomerId = original.CustomerId,
            CategoryId = original.CategoryId,
            CostCenterId = original.CostCenterId,
            ReversesEntryId = original.Id,
            Reason = RequireText(request.Reason, "entries.reversal.reason"),
            ActorUserId = actor
        };
        original.ReversalEntryId = reversal.Id;
        Touch(original);
        state.Entries.Add(reversal);
        state.Outbox.Add(new OutboxRecord
        {
            EventId = NewId("evt"),
            TenantId = tenantId,
            EntryId = reversal.Id,
            EventType = "cashflow.entry.reversed.v1"
        });
        Audit(state, tenantId, actor, "entries.reverse", original.Id);
        var response = JsonSerializer.Serialize(reversal);
        state.Idempotency.Add(new IdempotencyRecord
        {
            TenantId = tenantId,
            Operation = $"entries.reverse:{id}",
            Key = key,
            PayloadHash = hash,
            ResultJson = response,
            StatusCode = StatusCodes.Status201Created
        });
        return new IdempotencyOutcome(false, StatusCodes.Status201Created, response);
    }, ct);

    var reversalEntry = JsonSerializer.Deserialize<EntryRecord>(result.ResultJson)!;
    return result.Replayed ? Results.Ok(reversalEntry) : Results.Created($"/api/v1/entries/{reversalEntry.Id}", reversalEntry);
})
.WithSummary("Estorna lançamento confirmado gerando lançamento inverso.");

api.MapPost("/statements/exports", async (HttpContext http, FileCashFlowStore store, CreateStatementExportRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var format = NormalizeExportFormat(request.Format);
    var job = await store.MutateAsync(state =>
    {
        if (request.AccountId is not null)
        {
            state.Accounts.RequireTenant(tenantId, request.AccountId, "accounts");
        }

        var created = new StatementExportJob
        {
            Id = NewId("stx"),
            TenantId = tenantId,
            Format = format,
            RequestedBy = actor,
            From = request.From,
            To = request.To,
            AccountId = request.AccountId
        };
        state.StatementExports.Add(created);
        Audit(state, tenantId, actor, $"statements.export-{format}", created.Id);
        return created;
    }, ct);
    return Results.Accepted($"/api/v1/statements/exports/{job.Id}", job);
})
.WithSummary("Solicita geração assíncrona de extrato PDF, XLSX ou CSV.");

api.MapGet("/statements/exports/{id}", async (HttpContext http, FileCashFlowStore store, string id, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    var job = state.StatementExports.FirstOrDefault(item => item.TenantId == tenantId && item.Id == id);
    return job is null ? Results.NotFound() : Results.Ok(job);
})
.WithSummary("Consulta status de job de extrato.");

api.MapGet("/statements/exports/{id}/download", async (HttpContext http, FileCashFlowStore store, string id, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    var job = state.StatementExports.FirstOrDefault(item => item.TenantId == tenantId && item.Id == id);
    if (job is null)
    {
        return Results.NotFound();
    }

    if (job.Status != "Completed" || job.FileName is null || !File.Exists(job.FileName))
    {
        return Results.Problem("Extrato ainda não está pronto para download.", statusCode: StatusCodes.Status409Conflict);
    }

    var bytes = await File.ReadAllBytesAsync(job.FileName, ct);
    var downloadName = Path.GetFileName(job.FileName);
    return Results.File(bytes, job.MimeType ?? MediaTypeNames.Application.Octet, downloadName);
})
.WithSummary("Baixa extrato gerado com autorização repetida por tenant.");

api.MapGet("/audit", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Audit.Where(item => item.TenantId == tenantId).OrderByDescending(item => item.OccurredAt).Take(200));
})
.WithSummary("Lista trilha de auditoria sanitizada do tenant.");

api.MapGet("/internal/snapshot", async (FileCashFlowStore store, CancellationToken ct) =>
{
    var state = await store.ReadAsync(ct);
    return Results.Ok(new
    {
        state.UpdatedAt,
        entries = state.Entries.Count,
        outboxPending = state.Outbox.Count(item => item.PublishedAt is null),
        exportsPending = state.StatementExports.Count(item => item.Status == "Pending")
    });
})
.WithTags("Internal")
.WithSummary("Snapshot operacional sanitizado para workers do projeto.");

app.Run();

static string Tenant(HttpContext http)
    => http.Request.Headers.TryGetValue("X-Tenant-Id", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString()
        : SeedFactory.AlphaTenantId;

static string Actor(HttpContext http)
    => http.Request.Headers.TryGetValue("X-User-Id", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString()
        : SeedFactory.AdminUserId;

static string IdempotencyKey(HttpContext http)
{
    if (!http.Request.Headers.TryGetValue("Idempotency-Key", out var value) || string.IsNullOrWhiteSpace(value))
    {
        throw new CashFlowValidationException("idempotency.required", "Idempotency-Key é obrigatório para commands financeiros.");
    }

    return value.ToString();
}

static long ExpectedVersion(HttpContext http)
{
    if (!http.Request.Headers.TryGetValue("If-Match", out var value) || !long.TryParse(value.ToString().Trim('"'), out var version))
    {
        throw new CashFlowValidationException("etag.required", "If-Match com a versão atual é obrigatório.");
    }

    return version;
}

static void EnsureVersion(TenantEntity entity, long expected)
{
    if (entity.Version != expected)
    {
        throw new CashFlowValidationException("etag.conflict", "Versão divergente. Recarregue o recurso.");
    }
}

static void Touch(TenantEntity entity)
{
    entity.Version++;
    entity.UpdatedAt = DateTimeOffset.UtcNow;
}

static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

static string RequireText(string? value, string field)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new CashFlowValidationException($"{field}.required", "Campo obrigatório.");
    }

    return value.Trim();
}

static string NormalizeEntryType(string type)
{
    var normalized = type.Trim();
    return normalized.Equals("credit", StringComparison.OrdinalIgnoreCase) || normalized.Equals("credito", StringComparison.OrdinalIgnoreCase)
        ? "Credit"
        : normalized.Equals("debit", StringComparison.OrdinalIgnoreCase) || normalized.Equals("debito", StringComparison.OrdinalIgnoreCase)
            ? "Debit"
            : throw new CashFlowValidationException("entries.type.invalid", "Tipo deve ser Credit ou Debit.");
}

static string NormalizeExportFormat(string format)
{
    var normalized = format.Trim().ToLowerInvariant();
    return normalized is "pdf" or "xlsx" or "csv"
        ? normalized
        : throw new CashFlowValidationException("statements.format.invalid", "Formato deve ser pdf, xlsx ou csv.");
}

static void Audit(CashFlowState state, string tenantId, string actor, string action, string resourceId)
{
    state.Audit.Add(new AuditRecord
    {
        Id = NewId("aud"),
        TenantId = tenantId,
        ActorUserId = actor,
        Action = action,
        ResourceId = resourceId
    });
}

static IdempotencyOutcome? FindReplay(CashFlowState state, string tenantId, string operation, string key, string hash)
{
    var existing = state.Idempotency.FirstOrDefault(item => item.TenantId == tenantId && item.Operation == operation && item.Key == key);
    if (existing is null)
    {
        return null;
    }

    if (!string.Equals(existing.PayloadHash, hash, StringComparison.Ordinal))
    {
        throw new CashFlowValidationException("idempotency.payload_conflict", "A mesma Idempotency-Key foi usada com payload diferente.");
    }

    return new IdempotencyOutcome(true, existing.StatusCode, existing.ResultJson);
}

internal sealed record IdempotencyOutcome(bool Replayed, int StatusCode, string ResultJson);
internal sealed record UpsertCustomerRequest(string LegalName, string? TradeName, string? Email, string? Phone, bool RestrictedData);
internal sealed record UpsertAccountRequest(string Name, string? Currency, string? Description);
internal sealed record UpsertCategoryRequest(string Name, string Type);
internal sealed record UpsertCostCenterRequest(string Name);
internal sealed record PostEntryRequest(string AccountId, string Type, string Amount, DateOnly BusinessDate, string Description, string? CustomerId, string? CategoryId, string? CostCenterId);
internal sealed record ReverseEntryRequest(string Reason, DateOnly? BusinessDate);
internal sealed record CreateStatementExportRequest(string Format, DateOnly From, DateOnly To, string? AccountId);
