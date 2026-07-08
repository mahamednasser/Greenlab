// Layer: Infrastructure
// Responsibility: Implements lab device persistence with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class LabDeviceRepository : Repository<LabDevice>, ILabDeviceRepository
{
    public LabDeviceRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<LabDevice>> ListByLabIdAsync(int labId, CancellationToken cancellationToken = default)
    {
        return await DbContext.LabDevices
            .AsNoTracking()
            .Where(d => d.LabId == labId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<LabDevice?> GetByLabAndDeviceIdAsync(int labId, int deviceId, CancellationToken cancellationToken = default)
    {
        return DbContext.LabDevices.FirstOrDefaultAsync(d => d.LabId == labId && d.Id == deviceId, cancellationToken);
    }

    public Task<LabDevice?> GetByLabAndDeviceIdNoTrackingAsync(int labId, int deviceId, CancellationToken cancellationToken = default)
    {
        return DbContext.LabDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.LabId == labId && d.Id == deviceId, cancellationToken);
    }
}
