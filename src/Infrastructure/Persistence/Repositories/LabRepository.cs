// Layer: Infrastructure
// Responsibility: Implements lab persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class LabRepository : Repository<Lab>, ILabRepository
{
    public LabRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Lab>> ListAsync(bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? DbContext.Labs.AsQueryable() : DbContext.Labs.AsNoTracking();
        return await query.OrderBy(l => l.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Lab>> ListWithOverviewDataAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Labs
            .AsNoTracking()
            .Include(l => l.ControlOverride)
            .Include(l => l.SectionSchedules)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Lab?> GetWithDeviceStateAsync(int labId, CancellationToken cancellationToken = default)
    {
        return DbContext.Labs
            .Include(l => l.ControlOverride)
            .Include(l => l.Devices)
            .FirstOrDefaultAsync(l => l.Id == labId, cancellationToken);
    }

    public async Task<IReadOnlyList<Lab>> ListCurrentForStaffAsync(IReadOnlyCollection<int> labIds, CancellationToken cancellationToken = default)
    {
        return await DbContext.Labs
            .AsNoTracking()
            .Include(l => l.ControlOverride)
            .Include(l => l.SectionSchedules)
            .Where(l => labIds.Contains(l.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(int labId, CancellationToken cancellationToken = default)
    {
        return DbContext.Labs.AnyAsync(l => l.Id == labId, cancellationToken);
    }

    public Task<Lab?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return DbContext.Labs.FirstOrDefaultAsync(l => l.Name == name, cancellationToken);
    }

    public async Task DeleteCascadeAsync(int labId, CancellationToken cancellationToken = default)
    {
        await DbContext.LabSensors.Where(s => s.LabId == labId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await DbContext.LabChangeRequests.Where(r => r.FromLabId == labId || r.ToLabId == labId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await DbContext.Labs.Where(l => l.Id == labId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
