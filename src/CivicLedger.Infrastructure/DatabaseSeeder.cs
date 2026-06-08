using CivicLedger.Domain;
using Microsoft.EntityFrameworkCore;

namespace CivicLedger.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(CivicLedgerDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();
        if (await dbContext.Grants.AnyAsync())
        {
            return;
        }

        var currentYear = DateTime.UtcNow.Year;

        var transportation = new Grant(
            "Safe Streets Improvement Program",
            "Transportation",
            "Federal Highway Administration",
            850_000m,
            new DateOnly(currentYear, 1, 1),
            new DateOnly(currentYear, 12, 31));
        transportation.Activate();
        transportation.AddExpense(
            185_000m,
            "Construction",
            "West Texas Infrastructure",
            new DateOnly(currentYear, 2, 15));
        transportation.AddExpense(
            42_500m,
            "Engineering",
            "Civic Design Group",
            new DateOnly(currentYear, 3, 12));

        var community = new Grant(
            "Community Technology Access",
            "Community Services",
            "State Digital Equity Office",
            320_000m,
            new DateOnly(currentYear, 1, 1),
            new DateOnly(currentYear, 9, 30));
        community.Activate();
        community.AddExpense(
            118_000m,
            "Equipment",
            "Regional Technology Supply",
            new DateOnly(currentYear, 1, 28));
        community.AddExpense(
            96_000m,
            "Program Services",
            "Connected Communities",
            new DateOnly(currentYear, 2, 20));

        var water = new Grant(
            "Water Quality Monitoring",
            "Public Works",
            "Environmental Protection Agency",
            475_000m,
            new DateOnly(currentYear, 4, 1),
            new DateOnly(currentYear + 1, 3, 31));

        await dbContext.Grants.AddRangeAsync(transportation, community, water);
        await dbContext.SaveChangesAsync();
    }
}
