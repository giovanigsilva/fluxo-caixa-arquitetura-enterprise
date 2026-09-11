using Microsoft.AspNetCore.Mvc;
using Vertx.CashFlow.BuildingBlocks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(FileCashFlowStore.FromEnvironment(builder.Environment.EnvironmentName));

var app = builder.Build();
app.MapOpenApi();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", store = "file-backed" }));

var api = app.MapGroup("/api/v1").WithTags("Management");

api.MapGet("/permissions", () => Results.Ok(new
{
    catalogVersion = 1,
    permissions = PermissionCatalog.Version1
}))
.WithSummary("Retorna catálogo versionado de permissões allowlist.");

api.MapGet("/capabilities", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var userId = Actor(http);
    var state = await store.ReadAsync(ct);
    var user = state.Users.FirstOrDefault(item => item.TenantId == tenantId && item.Id == userId && !item.Disabled);
    var permissions = user is null
        ? Array.Empty<string>()
        : state.Roles
            .Where(role => role.TenantId == tenantId && user.RoleIds.Contains(role.Id) && !role.IsDeleted)
            .SelectMany(role => role.Permissions)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    return Results.Ok(new { tenantId, userId, permissions, expiresInSeconds = 30 });
})
.WithSummary("Retorna capabilities efetivas para menus, botões e colunas.");

api.MapGet("/users", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Users.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderBy(item => item.DisplayName));
})
.WithSummary("Lista usuários de aplicação do tenant.");

api.MapPost("/users", async (HttpContext http, FileCashFlowStore store, UpsertUserRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var user = await store.MutateAsync(state =>
    {
        var roleIds = request.RoleIds.Where(roleId => state.Roles.Any(role => role.TenantId == tenantId && role.Id == roleId)).Distinct(StringComparer.Ordinal).ToList();
        var created = new UserRecord
        {
            Id = $"user_{Guid.NewGuid():N}",
            TenantId = tenantId,
            Username = Required(request.Username, "users.username"),
            DisplayName = Required(request.DisplayName, "users.display_name"),
            RoleIds = roleIds
        };
        state.Users.Add(created);
        state.Audit.Add(new AuditRecord
        {
            Id = $"aud_{Guid.NewGuid():N}",
            TenantId = tenantId,
            ActorUserId = actor,
            Action = "users.invite",
            ResourceId = created.Id
        });
        return created;
    }, ct);
    return Results.Created($"/api/v1/users/{user.Id}", user);
})
.WithSummary("Cria usuário de aplicação sem manipular credenciais do Keycloak.");

api.MapGet("/roles", async (HttpContext http, FileCashFlowStore store, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var state = await store.ReadAsync(ct);
    return Results.Ok(state.Roles.Where(item => item.TenantId == tenantId && !item.IsDeleted).OrderBy(item => item.Name));
})
.WithSummary("Lista perfis editáveis por tenant.");

api.MapPost("/roles", async (HttpContext http, FileCashFlowStore store, UpsertRoleRequest request, CancellationToken ct) =>
{
    var tenantId = Tenant(http);
    var actor = Actor(http);
    var invalid = request.Permissions.Except(PermissionCatalog.Version1, StringComparer.Ordinal).ToArray();
    if (invalid.Length > 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["permissions"] = invalid });
    }

    var role = await store.MutateAsync(state =>
    {
        var created = new RoleRecord
        {
            Id = $"role_{Guid.NewGuid():N}",
            TenantId = tenantId,
            Name = Required(request.Name, "roles.name"),
            Permissions = request.Permissions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList()
        };
        state.Roles.Add(created);
        state.Audit.Add(new AuditRecord
        {
            Id = $"aud_{Guid.NewGuid():N}",
            TenantId = tenantId,
            ActorUserId = actor,
            Action = "roles.manage",
            ResourceId = created.Id
        });
        return created;
    }, ct);
    return Results.Created($"/api/v1/roles/{role.Id}", role);
})
.WithSummary("Cria role com permissões allowlist.");

app.Run();

static string Tenant(HttpContext http)
    => http.Request.Headers.TryGetValue("X-Tenant-Id", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString()
        : SeedFactory.AlphaTenantId;

static string Actor(HttpContext http)
    => http.Request.Headers.TryGetValue("X-User-Id", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString()
        : SeedFactory.AdminUserId;

static string Required(string? value, string field)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new BadHttpRequestException($"{field} é obrigatório.");
    }

    return value.Trim();
}

internal sealed record UpsertUserRequest(string Username, string DisplayName, string[] RoleIds);
internal sealed record UpsertRoleRequest(string Name, string[] Permissions);
