using KmValidator.Domain.Entities;
using KmValidator.Domain.Ports;
using KmValidator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmValidator.Infrastructure.Repositories;

public class WearableDeviceRepository : IWearableDeviceRepository
{
    private readonly KmValidatorDbContext _context;

    public WearableDeviceRepository(KmValidatorDbContext context)
    {
        _context = context;
    }

    public async Task<WearableDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WearableDevices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<WearableDevice?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        return await _context.WearableDevices.FirstOrDefaultAsync(d => d.SerialNumber == serialNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<WearableDevice>> GetByAthleteIdAsync(Guid athleteId, CancellationToken cancellationToken = default)
    {
        return await _context.WearableDevices
            .Where(d => d.AthleteId == athleteId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WearableDevice device, CancellationToken cancellationToken = default)
    {
        await _context.WearableDevices.AddAsync(device, cancellationToken);
    }

    public Task UpdateAsync(WearableDevice device, CancellationToken cancellationToken = default)
    {
        _context.WearableDevices.Update(device);
        return Task.CompletedTask;
    }
}
