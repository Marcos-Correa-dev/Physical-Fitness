using KmValidator.Domain.Entities;

namespace KmValidator.Domain.Ports;

public interface IPhysicalActivityRepository
{
    Task<PhysicalActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalActivity>> GetAllRecentAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalActivity>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task AddAsync(PhysicalActivity activity, CancellationToken cancellationToken = default);
    Task UpdateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default);
    Task DeleteAsync(PhysicalActivity activity, CancellationToken cancellationToken = default);
}
