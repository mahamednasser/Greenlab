// Layer: Infrastructure
// Responsibility: Implements lab change request persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class LabChangeRequestRepository : Repository<LabChangeRequest>, ILabChangeRequestRepository
{
    public LabChangeRequestRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<LabChangeRequest>> ListAsync(LabChangeRequestStatus? status, CancellationToken cancellationToken = default)
    {
        var query = DbContext.LabChangeRequests
            .AsNoTracking()
            .Include(r => r.RequestedBy)
            .Include(r => r.FromLab)
            .Include(r => r.ToLab)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LabChangeRequest>> ListByRequesterAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.LabChangeRequests
            .AsNoTracking()
            .Include(r => r.RequestedBy)
            .Include(r => r.FromLab)
            .Include(r => r.ToLab)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<LabChangeRequest?> GetForReviewAsync(int id, CancellationToken cancellationToken = default)
    {
        return DbContext.LabChangeRequests
            .Include(x => x.RequestedBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
