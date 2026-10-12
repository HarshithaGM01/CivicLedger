using CivicLedger.Domain;
using CivicLedger.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CivicLedger.Tests;

public sealed class TransactionBoundaryTests
{
    private static Grant NewGrant() => new("Test", "Finance", "Sample", 100m,
        new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.001)]
    [InlineData(1.005)]
    public void MoneyRejectsNonpositiveOrFractionalCents(double value)
    {
        var amount = (decimal)value;
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grant("Test", "Finance", "Sample",
            amount, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
        var grant = NewGrant();
        grant.Activate();
        var auditCount = grant.AuditEntries.Count;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            grant.AddExpense(amount, "Supplies", "Vendor", grant.StartDate));
        Assert.Empty(grant.Expenses);
        Assert.Equal(auditCount, grant.AuditEntries.Count);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 31)]
    public void ExactBudgetAndInclusivePeriodBoundariesAreAccepted(int month, int day)
    {
        var grant = NewGrant();
        grant.Activate();
        grant.AddExpense(100m, "Supplies", "Vendor", new DateOnly(2026, month, day));
        Assert.Equal(0m, grant.RemainingAmount);
        Assert.Throws<InvalidOperationException>(() =>
            grant.AddExpense(0.01m, "Supplies", "Vendor", grant.StartDate));
        Assert.Single(grant.Expenses);
    }

    [Fact]
    public void InvalidTextAndDatesDoNotMutateExpensesOrAudit()
    {
        var grant = NewGrant();
        grant.Activate();
        var auditCount = grant.AuditEntries.Count;
        Assert.Throws<ArgumentException>(() => grant.AddExpense(1m, " ", "Vendor", grant.StartDate));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            grant.AddExpense(1m, "Supplies", "Vendor", grant.StartDate.AddDays(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            grant.AddExpense(1m, "Supplies", "Vendor", grant.EndDate.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() => grant.Activate());
        Assert.Empty(grant.Expenses);
        Assert.Equal(auditCount, grant.AuditEntries.Count);
    }

    [Fact]
    public async Task StaleExpenseWriteRollsBackItsExpenseAndAudit()
    {
        // Two independently loaded contexts simulate requests sharing a stale budget.
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CivicLedgerDbContext>().UseSqlite(connection).Options;
        await using var setup = new CivicLedgerDbContext(options);
        await setup.Database.EnsureCreatedAsync();
        var grant = NewGrant();
        grant.Activate();
        setup.Grants.Add(grant);
        await setup.SaveChangesAsync();

        await using var first = new CivicLedgerDbContext(options);
        await using var second = new CivicLedgerDbContext(options);
        var a = await first.Grants.Include(g => g.Expenses).Include(g => g.AuditEntries).SingleAsync();
        var b = await second.Grants.Include(g => g.Expenses).Include(g => g.AuditEntries).SingleAsync();
        a.AddExpense(60m, "Supplies", "First", a.StartDate);
        b.AddExpense(60m, "Supplies", "Second", b.StartDate);
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var verify = new CivicLedgerDbContext(options);
        var saved = await verify.Grants.Include(g => g.Expenses).Include(g => g.AuditEntries).SingleAsync();
        Assert.Equal(60m, saved.SpentAmount);
        Assert.Single(saved.Expenses);
        Assert.Single(saved.AuditEntries.Where(e => e.Action == "Expense recorded"));
    }
}
