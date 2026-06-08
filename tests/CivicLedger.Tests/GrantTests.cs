using CivicLedger.Domain;

namespace CivicLedger.Tests;

public sealed class GrantTests
{
    [Fact]
    public void AddExpense_UpdatesBudgetAndAuditHistory()
    {
        var grant = CreateGrant();
        grant.Activate();

        grant.AddExpense(
            25_000m,
            "Equipment",
            "Civic Supply",
            new DateOnly(2026, 3, 1));

        Assert.Equal(25_000m, grant.SpentAmount);
        Assert.Equal(75_000m, grant.RemainingAmount);
        Assert.Equal(25m, grant.UtilizationPercent);
        Assert.Contains(grant.AuditEntries, entry => entry.Action == "Expense recorded");
    }

    [Fact]
    public void AddExpense_RejectsExpenseAboveRemainingBudget()
    {
        var grant = CreateGrant();
        grant.Activate();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            grant.AddExpense(
                100_001m,
                "Construction",
                "Civic Supply",
                new DateOnly(2026, 3, 1)));

        Assert.Contains("remaining grant budget", exception.Message);
    }

    [Fact]
    public void AddExpense_RejectsDraftGrant()
    {
        var grant = CreateGrant();

        Assert.Throws<InvalidOperationException>(() =>
            grant.AddExpense(
                500m,
                "Supplies",
                "Civic Supply",
                new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Constructor_RejectsInvalidGrantPeriod()
    {
        Assert.Throws<ArgumentException>(() =>
            new Grant(
                "Invalid grant",
                "Finance",
                "State",
                100_000m,
                new DateOnly(2026, 12, 31),
                new DateOnly(2026, 1, 1)));
    }

    private static Grant CreateGrant() =>
        new(
            "Public Safety Equipment",
            "Public Safety",
            "State Resilience Fund",
            100_000m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
}
