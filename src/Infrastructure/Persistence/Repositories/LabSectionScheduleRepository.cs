// Layer: Infrastructure
// Responsibility: Implements lab section schedule persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class LabSectionScheduleRepository : Repository<LabSectionSchedule>, ILabSectionScheduleRepository
{
    public LabSectionScheduleRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<bool> HasOverlapAsync(int labId, DateTime sectionStart, DateTime sectionEnd, CancellationToken cancellationToken = default)
    {
        var day = sectionStart.DayOfWeek;
        var start = sectionStart.TimeOfDay;
        var end = sectionEnd.TimeOfDay;
        return DbContext.LabSectionSchedules.AsNoTracking().AnyAsync(s =>
            s.LabId == labId
            && s.IsActive
            && s.DayOfWeek == day
            && s.StartTime < end
            && s.EndTime > start,
            cancellationToken);
    }
}
