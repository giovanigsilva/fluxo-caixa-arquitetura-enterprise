using Vertx.CashFlow.BuildingBlocks;

namespace Vertx.CashFlow.SecurityTests;

public sealed class SecurityInvariantTests
{
    [Fact]
    public void Permission_catalog_contains_required_entry_and_export_permissions()
    {
        Assert.Contains("entries.post", PermissionCatalog.Version1);
        Assert.Contains("entries.reverse", PermissionCatalog.Version1);
        Assert.Contains("statements.export-pdf", PermissionCatalog.Version1);
        Assert.Contains("statements.export-xlsx", PermissionCatalog.Version1);
        Assert.Contains("statements.export-csv", PermissionCatalog.Version1);
    }

    [Fact]
    public void Tenant_lookup_rejects_idor_between_seed_tenants()
    {
        var state = SeedFactory.CreateUatState();
        var betaCustomer = state.Customers.First(customer => customer.TenantId == SeedFactory.BetaTenantId);

        Assert.Throws<CashFlowValidationException>(() =>
            state.Customers.RequireTenant(SeedFactory.AlphaTenantId, betaCustomer.Id, "customers"));
    }
}
