// Layer: Infrastructure
// Responsibility: Implements staff lab assignment persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class StaffLabAssignmentRepository : Repository<StaffLabAssignment>, IStaffLabAssignmentRepository
{
    public StaffLabAssignmentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<StaffLabAssignment>> ListByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.StaffLabAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
