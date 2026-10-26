using KmValidator.Domain.Entities;

namespace KmValidator.Domain.Ports;

public interface IWearableDeviceRepository
{
    Task<WearableDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WearableDevice?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WearableDevice>> GetByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken = default);
    Task AddAsync(WearableDevice device, CancellationToken cancellationToken = default);
    Task UpdateAsync(WearableDevice device, CancellationToken cancellationToken = default);
}
