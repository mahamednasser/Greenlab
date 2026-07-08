// Layer: Application
// Responsibility: Defines request and response DTOs for ScheduleDtos use cases.
using System.ComponentModel.DataAnnotations;

namespace Greenlab.Application.DTOs;

public class ScheduleRowImportDto
{
    [Required]
    public int LabId { get; set; }
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public TimeSpan StartTime { get; set; }
    [Required]
    public TimeSpan EndTime { get; set; }
    public string? CourseName { get; set; }
}

public class StaffAssignmentImportDto
{
    [Required, EmailAddress]
    public string StaffEmail { get; set; } = string.Empty;
    [Required]
    public int LabId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class BulkScheduleImportRequest
{
    public List<ScheduleRowImportDto> LabSections { get; set; } = new();
    public List<StaffAssignmentImportDto> StaffAssignments { get; set; } = new();
}
