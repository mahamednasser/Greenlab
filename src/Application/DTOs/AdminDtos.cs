// Layer: Application
// Responsibility: Defines request and response DTOs for AdminDtos use cases.
using System.ComponentModel.DataAnnotations;
using Greenlab.Domain.Enums;

namespace Greenlab.Application.DTOs;

public class LabOverviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LastOccupancyCount { get; set; }
    public DateTime? LastOccupancyAt { get; set; }
    public bool InSession { get; set; }
    public LabOverrideDto? Override { get; set; }
    public bool ActualLightOn { get; set; }
    public bool ActualFanOn { get; set; }
    /// <summary>Accumulated light ON time (seconds), including current ON interval.</summary>
    public double LightRuntimeSeconds { get; set; }
    /// <summary>Accumulated fan ON time (seconds), including current ON interval.</summary>
    public double FanRuntimeSeconds { get; set; }
    /// <summary>Instantaneous load: light 20 W + fan 70 W when ON.</summary>
    public double CurrentWatts { get; set; }
    /// <summary>Energy from runtime: (lightH×0.02) + (fanH×0.07) kWh.</summary>
    public double EnergyKwh { get; set; }
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

    [MinLength(6), MaxLength(128)]
    public string? Password { get; set; }
}

public class LabSectionListItemDto
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public string LabName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? CourseName { get; set; }
}

public class LabOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
    /// <summary>0=Sunday … 6=Saturday. Serialized as int so the UI does not misread enum names.</summary>
    public int? DayOfWeek { get; set; }
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

    /// <summary>0=Sunday … 6=Saturday.</summary>
    public int? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? LabId { get; set; }
    public string? LabName { get; set; }
    public int? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}

