// Layer: Application
// Responsibility: Evaluates staff access to labs from assignment rows.
using Greenlab.Domain.Entities;

namespace Greenlab.Application.Common;

internal static class StaffAccessEvaluator
{
    public static IReadOnlyList<int> GetAllowedLabIds(IEnumerable<StaffLabAssignment> assignments, DateTime utcNow)
    {
        var assignmentList = assignments.ToList();
        var temporary = assignmentList.Where(a =>
            a.ValidFrom.HasValue && a.ValidUntil.HasValue
            && utcNow >= a.ValidFrom.Value && utcNow <= a.ValidUntil.Value).ToList();

        if (temporary.Count > 0)
            return temporary.Select(a => a.LabId).Distinct().ToList();

        var list = new HashSet<int>();
        foreach (var assignment in assignmentList)
        {
            if (!assignment.DayOfWeek.HasValue || !assignment.StartTime.HasValue || !assignment.EndTime.HasValue)
                continue;

            if (utcNow.DayOfWeek != assignment.DayOfWeek.Value)
                continue;

            var timeOfDay = utcNow.TimeOfDay;
            if (timeOfDay >= assignment.StartTime.Value && timeOfDay <= assignment.EndTime.Value)
                list.Add(assignment.LabId);
        }

        return list.ToList();
    }
}
