using Greenlab.Data;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Services;

public class LabAccessService : ILabAccessService
{
    private readonly AppDbContext _db;

    public LabAccessService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<int>> GetStaffAllowedLabIdsAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var assignments = await _db.StaffLabAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var temporary = assignments.Where(a =>
            a.ValidFrom.HasValue && a.ValidUntil.HasValue
            && utcNow >= a.ValidFrom.Value && utcNow <= a.ValidUntil.Value).ToList();

        if (temporary.Count > 0)
            return temporary.Select(a => a.LabId).Distinct().ToList();

        var list = new HashSet<int>();
        foreach (var a in assignments)
        {
            if (a.DayOfWeek.HasValue && a.StartTime.HasValue && a.EndTime.HasValue)
            {
                if (utcNow.DayOfWeek != a.DayOfWeek.Value)
                    continue;
                var tod = utcNow.TimeOfDay;
                if (tod >= a.StartTime.Value && tod <= a.EndTime.Value)
                    list.Add(a.LabId);
            }
        }

        return list.ToList();
    }

    public async Task<bool> CanStaffAccessLabAsync(int userId, int labId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var ids = await GetStaffAllowedLabIdsAsync(userId, utcNow, cancellationToken).ConfigureAwait(false);
        return ids.Contains(labId);
    }
}
