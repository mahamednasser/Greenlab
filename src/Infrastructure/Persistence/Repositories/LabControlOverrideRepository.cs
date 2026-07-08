// Layer: Infrastructure
// Responsibility: Implements lab control override persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class LabControlOverrideRepository : Repository<LabControlOverride>, ILabControlOverrideRepository
{
    public LabControlOverrideRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<LabControlOverride?> GetByLabIdAsync(int labId, CancellationToken cancellationToken = default)
    {
        return DbContext.LabControlOverrides.FirstOrDefaultAsync(o => o.LabId == labId, cancellationToken);
    }
}
