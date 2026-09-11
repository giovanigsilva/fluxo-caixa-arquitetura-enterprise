using Vertx.CashFlow.BuildingBlocks;

namespace Vertx.CashFlow.UnitTests;

public sealed class CashFlowPrimitiveTests
{
    [Fact]
    public void Money_accepts_positive_canonical_two_decimal_values()
    {
        var money = Money.ParsePositive("1234.50");

        Assert.Equal("1234.50", money.ToCanonicalString());
    }

    [Theory]
    [InlineData("0.00")]
    [InlineData("-1.00")]
    [InlineData("10.999")]
    [InlineData("abc")]
    public void Money_rejects_invalid_financial_values(string value)
    {
        Assert.Throws<CashFlowValidationException>(() => Money.ParsePositive(value));
    }

    [Fact]
    public void Uat_seed_keeps_tenants_separated()
    {
        var state = SeedFactory.CreateUatState();

        Assert.Contains(state.Accounts, account => account.TenantId == SeedFactory.AlphaTenantId);
        Assert.Contains(state.Accounts, account => account.TenantId == SeedFactory.BetaTenantId);
        Assert.DoesNotContain(state.Customers.Where(customer => customer.TenantId == SeedFactory.AlphaTenantId), customer => customer.TenantId == SeedFactory.BetaTenantId);
    }
}
