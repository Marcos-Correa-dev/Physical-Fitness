using KmValidator.Domain.Entities;

namespace KmValidator.Domain.Ports;

public interface IAthleteRepository
{
    Task<Athlete?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Athlete?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Athlete?> GetDefaultAthleteAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Athlete athlete, CancellationToken cancellationToken = default);
}
