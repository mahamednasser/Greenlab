// Layer: Domain
// Responsibility: Defines the LabSectionSchedule domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

public class LabSectionSchedule
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? CourseName { get; set; }
    public string? SectionName { get; set; }
    public string? AcademicTerm { get; set; }
    public bool IsActive { get; set; } = true;
}
