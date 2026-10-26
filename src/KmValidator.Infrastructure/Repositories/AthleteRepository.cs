using KmValidator.Domain.Entities;
using KmValidator.Domain.Ports;
using KmValidator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmValidator.Infrastructure.Repositories;

public class AthleteRepository : IAthleteRepository
{
    private readonly KmValidatorDbContext _context;

    public AthleteRepository(KmValidatorDbContext context)
    {
        _context = context;
    }

    public async Task<Athlete?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Athletes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Athlete?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Athletes.FirstOrDefaultAsync(a => a.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<Athlete?> GetDefaultAthleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Athletes.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Athlete athlete, CancellationToken cancellationToken = default)
    {
        await _context.Athletes.AddAsync(athlete, cancellationToken);
    }
}
