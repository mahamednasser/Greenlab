// Layer: Infrastructure
// Responsibility: Implements smart AC persistence with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class SmartAcRepository : Repository<SmartAc>, ISmartAcRepository
{
    public SmartAcRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<SmartAc>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.SmartAcs
            .AsNoTracking()
            .Include(a => a.Lab)
            .OrderBy(a => a.Lab.Name)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SmartAc>> ListByLabIdAsync(int labId, CancellationToken cancellationToken = default)
    {
        return await DbContext.SmartAcs
            .AsNoTracking()
            .Include(a => a.Lab)
            .Where(a => a.LabId == labId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<SmartAc?> GetByLabAndIdAsync(int labId, int acId, CancellationToken cancellationToken = default)
    {
        return DbContext.SmartAcs.FirstOrDefaultAsync(a => a.LabId == labId && a.Id == acId, cancellationToken);
    }

    public Task<SmartAc?> GetByIdWithLabAsync(int acId, CancellationToken cancellationToken = default)
    {
        return DbContext.SmartAcs
            .Include(a => a.Lab)
            .FirstOrDefaultAsync(a => a.Id == acId, cancellationToken);
    }
}
