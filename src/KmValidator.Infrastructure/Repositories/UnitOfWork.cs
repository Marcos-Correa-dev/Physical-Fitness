using KmValidator.Domain.Ports;
using KmValidator.Infrastructure.Persistence;

namespace KmValidator.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly KmValidatorDbContext _context;

    public UnitOfWork(KmValidatorDbContext context)
    {
        _context = context;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
