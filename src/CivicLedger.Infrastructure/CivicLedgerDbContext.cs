using CivicLedger.Domain;
using Microsoft.EntityFrameworkCore;

namespace CivicLedger.Infrastructure;

public sealed class CivicLedgerDbContext(DbContextOptions<CivicLedgerDbContext> options)
    : DbContext(options)
{
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Grant>(builder =>
        {
            builder.HasKey(grant => grant.Id);
            builder.Property(grant => grant.Name).HasMaxLength(140).IsRequired();
            builder.Property(grant => grant.Department).HasMaxLength(100).IsRequired();
            builder.Property(grant => grant.FundingSource).HasMaxLength(120).IsRequired();
            builder.Property(grant => grant.AwardedAmount).HasPrecision(18, 2);
            builder.Property(grant => grant.Status).HasConversion<string>().HasMaxLength(20);
            builder.Ignore(grant => grant.SpentAmount);
            builder.Ignore(grant => grant.RemainingAmount);
            builder.Ignore(grant => grant.UtilizationPercent);

            builder.HasMany(grant => grant.Expenses)
                .WithOne()
                .HasForeignKey(expense => expense.GrantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(grant => grant.AuditEntries)
                .WithOne()
                .HasForeignKey(entry => entry.GrantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(grant => grant.Expenses)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(grant => grant.AuditEntries)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Expense>(builder =>
        {
            builder.HasKey(expense => expense.Id);
            builder.Property(expense => expense.Amount).HasPrecision(18, 2);
            builder.Property(expense => expense.Category).HasMaxLength(80).IsRequired();
            builder.Property(expense => expense.Vendor).HasMaxLength(120).IsRequired();
            builder.HasIndex(expense => new { expense.GrantId, expense.IncurredOn });
        });

        modelBuilder.Entity<AuditEntry>(builder =>
        {
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Action).HasMaxLength(100).IsRequired();
            builder.Property(entry => entry.Details).HasMaxLength(600).IsRequired();
            builder.HasIndex(entry => new { entry.GrantId, entry.CreatedAtUtc });
        });
    }
}
