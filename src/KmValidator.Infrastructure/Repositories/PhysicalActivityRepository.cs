using KmValidator.Domain.Entities;
using KmValidator.Domain.Ports;
using KmValidator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmValidator.Infrastructure.Repositories;

public class PhysicalActivityRepository : IPhysicalActivityRepository
{
    private readonly KmValidatorDbContext _context;

    public PhysicalActivityRepository(KmValidatorDbContext context)
    {
        _context = context;
    }

    public async Task<PhysicalActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PhysicalActivities
            .Include(a => a.RoutePoints)
            .Include(a => a.Validation)
                .ThenInclude(v => v!.Issues)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalActivity>> GetAllRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.PhysicalActivities
            .Include(a => a.RoutePoints)
            .Include(a => a.Validation)
                .ThenInclude(v => v!.Issues)
            .OrderByDescending(a => a.StartTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhysicalActivity>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return await _context.PhysicalActivities
            .Include(a => a.RoutePoints)
            .Include(a => a.Validation)
                .ThenInclude(v => v!.Issues)
            .Where(a => a.StartTime >= fromUtc && a.StartTime <= toUtc)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        await _context.PhysicalActivities.AddAsync(activity, cancellationToken);
    }

    public Task UpdateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        _context.PhysicalActivities.Update(activity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        _context.PhysicalActivities.Remove(activity);
        return Task.CompletedTask;
    }
}
