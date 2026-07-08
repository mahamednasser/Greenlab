// Layer: Infrastructure
// Responsibility: Implements schedule image import persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class ScheduleImageImportRepository : Repository<ScheduleImageImport>, IScheduleImageImportRepository
{
    public ScheduleImageImportRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<ScheduleImageImport>> ListRecentAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.ScheduleImageImports
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
