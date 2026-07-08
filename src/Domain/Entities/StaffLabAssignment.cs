// Layer: Domain
// Responsibility: Defines the StaffLabAssignment domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

/// <summary>
/// Weekly slot (DayOfWeek + times) or a temporary window (ValidFrom–ValidUntil) after admin approves lab change.
/// </summary>
public class StaffLabAssignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    /// <summary>When set with StartTime/EndTime, repeats weekly. Ignored for date-range-only rows.</summary>
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    /// <summary>Temporary assignment: active between these UTC instants (e.g. approved section in another lab).</summary>
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public StaffAssignmentSource AssignmentSource { get; set; } = StaffAssignmentSource.ScheduleImport;
    public bool IsActive { get; set; } = true;
}
