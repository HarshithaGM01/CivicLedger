using CivicLedger.Application;
using CivicLedger.Domain;
using Microsoft.EntityFrameworkCore;

namespace CivicLedger.Infrastructure;

public sealed class GrantRepository(CivicLedgerDbContext dbContext) : IGrantRepository
{
    public async Task<IReadOnlyCollection<Grant>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Grants
            .AsNoTracking()
            .Include(grant => grant.Expenses)
            .Include(grant => grant.AuditEntries)
            .ToArrayAsync(cancellationToken);

    public Task<Grant?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Grants
            .Include(grant => grant.Expenses)
            .Include(grant => grant.AuditEntries)
            .SingleOrDefaultAsync(grant => grant.Id == id, cancellationToken);

    public async Task AddAsync(Grant grant, CancellationToken cancellationToken = default) =>
        await dbContext.Grants.AddAsync(grant, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
