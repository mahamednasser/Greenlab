using System.ComponentModel.DataAnnotations;

namespace Greenlab.DTOs;

public class ProposedLabDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? CvLabCode { get; set; }
    public string? DeviceKey { get; set; }
}

public class ProposedSectionDto
{
    [Required]
    public string LabName { get; set; } = string.Empty;
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public TimeSpan StartTime { get; set; }
    [Required]
    public TimeSpan EndTime { get; set; }
    public string? CourseName { get; set; }
}

public class ProposedStaffFromImageDto
{
    [Required]
    public string PersonName { get; set; } = string.Empty;

    /// <summary>Teacher or TeacherAssistant</summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string LabName { get; set; } = string.Empty;
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public TimeSpan StartTime { get; set; }
    [Required]
    public TimeSpan EndTime { get; set; }
}

/// <summary>Returned by vision analysis; same shape can be sent to apply after edits.</summary>
public class ScheduleAnalysisResultDto
{
    public List<ProposedLabDto> Labs { get; set; } = new();
    public List<ProposedSectionDto> Sections { get; set; } = new();
    public List<ProposedStaffFromImageDto> StaffAssignments { get; set; } = new();
    public string? Notes { get; set; }
}

public class ApplyScheduleAnalysisRequest : ScheduleAnalysisResultDto
{
    /// <summary>When true, create Teacher/TeacherAssistant users that do not exist (email derived from name).</summary>
    public bool CreateMissingUsers { get; set; } = true;
}

public class ApplyScheduleAnalysisResultDto
{
    public int LabsCreated { get; set; }
    public int LabsMatchedExisting { get; set; }
    public int SectionsAdded { get; set; }
    public int UsersCreated { get; set; }
    public int StaffAssignmentsAdded { get; set; }
    public List<string> Warnings { get; set; } = new();
}
