// Layer: Application
// Responsibility: Evaluates staff access to labs from assignment rows.
using Greenlab.Domain.Entities;

namespace Greenlab.Application.Common;

public static class StaffAccessEvaluator
{
    /// <summary>
    /// Returns lab ids the staff member may control right now.
    /// Temporary ValidFrom/ValidUntil rows use UTC; weekly DayOfWeek+times use local campus time.
    /// When any temporary window is active, it takes precedence over weekly slots.
    /// </summary>
    public static IReadOnlyList<int> GetAllowedLabIds(
        IEnumerable<StaffLabAssignment> assignments,
        DateTime utcNow,
        string? timeZoneId = null)
    {
        var assignmentList = assignments.Where(a => a.IsActive).ToList();
        var utc = utcNow.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)
            : utcNow.ToUniversalTime();

        var temporary = assignmentList.Where(a =>
            a.ValidFrom.HasValue && a.ValidUntil.HasValue
            && utc >= a.ValidFrom.Value.ToUniversalTime()
            && utc <= a.ValidUntil.Value.ToUniversalTime()).ToList();

        if (temporary.Count > 0)
            return temporary.Select(a => a.LabId).Distinct().ToList();

        var localNow = ScheduleClock.ToLocal(utc, timeZoneId);
        var list = new HashSet<int>();
        foreach (var assignment in assignmentList)
        {
            if (!assignment.DayOfWeek.HasValue || !assignment.StartTime.HasValue || !assignment.EndTime.HasValue)
                continue;

            if (localNow.DayOfWeek != assignment.DayOfWeek.Value)
                continue;

            var timeOfDay = localNow.TimeOfDay;
            if (timeOfDay >= assignment.StartTime.Value && timeOfDay <= assignment.EndTime.Value)
                list.Add(assignment.LabId);
        }

        return list.ToList();
    }
}
