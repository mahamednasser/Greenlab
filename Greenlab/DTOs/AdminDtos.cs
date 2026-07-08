using System.ComponentModel.DataAnnotations;
using Greenlab.Models;

namespace Greenlab.DTOs;

public class LabOverviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LastOccupancyCount { get; set; }
    public DateTime? LastOccupancyAt { get; set; }
    public bool InSession { get; set; }
    public LabOverrideDto? Override { get; set; }
}

public class LabOverrideDto
{
    public bool MotionSensorDisabled { get; set; }
    public bool? ForceLightOn { get; set; }
    public bool? ForceFanOn { get; set; }
    public bool? ForceRelay3On { get; set; }
    public bool? ForceRelay4On { get; set; }
}

public class SetLabOverrideRequest
{
    public bool? MotionSensorDisabled { get; set; }
    public bool? ForceLightOn { get; set; }
    public bool? ForceFanOn { get; set; }
    public bool? ForceRelay3On { get; set; }
    public bool? ForceRelay4On { get; set; }
}

public class LabChangeRequestDto
{
    public int Id { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public int FromLabId { get; set; }
    public string FromLabName { get; set; } = string.Empty;
    public int ToLabId { get; set; }
    public string ToLabName { get; set; } = string.Empty;
    public DateTime SectionStart { get; set; }
    public DateTime SectionEnd { get; set; }
    public LabChangeRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public class ReviewLabChangeRequest
{
    public bool Approve { get; set; }
    public string? Note { get; set; }
}

public class CreateLabChangeRequestDto
{
    public int FromLabId { get; set; }
    public int ToLabId { get; set; }
    public DateTime SectionStart { get; set; }
    public DateTime SectionEnd { get; set; }
}

public class UserListItemDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>If set, replaces the password.</summary>
    [MinLength(6), MaxLength(128)]
    public string? Password { get; set; }
}

public class LabSectionListItemDto
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public string LabName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? CourseName { get; set; }
}

public class StaffAssignmentListItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string StaffEmail { get; set; } = string.Empty;
    public string StaffRole { get; set; } = string.Empty;
    public int LabId { get; set; }
    public string LabName { get; set; } = string.Empty;
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public StaffAssignmentSource AssignmentSource { get; set; }
    public bool IsActive { get; set; }
}

public class CreateStaffAssignmentRequest
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
