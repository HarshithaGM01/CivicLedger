using CivicLedger.Domain;

namespace CivicLedger.Application;

public interface IGrantRepository
{
    Task<IReadOnlyCollection<Grant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Grant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Grant grant, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
