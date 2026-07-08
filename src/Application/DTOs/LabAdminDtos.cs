// Layer: Application
// Responsibility: Defines request and response DTOs for LabAdminDtos use cases.
using System.ComponentModel.DataAnnotations;
using Greenlab.Domain.Enums;

namespace Greenlab.Application.DTOs;

public class LabDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceKey { get; set; } = string.Empty;
    public int LightThreshold { get; set; }
    public float TempFanOnThresholdCelsius { get; set; }
    public string? CvLabCode { get; set; }
    public string? CameraRtspUrl { get; set; }
}

public class CreateLabRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>If omitted, a random key is generated.</summary>
    [MaxLength(128)]
    public string? DeviceKey { get; set; }

    public int LightThreshold { get; set; } = 2000;
    public float TempFanOnThresholdCelsius { get; set; } = 28f;
    public string? CvLabCode { get; set; }
    public string? CameraRtspUrl { get; set; }
}

public class UpdateLabRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string DeviceKey { get; set; } = string.Empty;

    public int LightThreshold { get; set; }
    public float TempFanOnThresholdCelsius { get; set; }
    public string? CvLabCode { get; set; }
    public string? CameraRtspUrl { get; set; }
}

public class LabSectionSlotDto
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    [Required]
    public TimeSpan StartTime { get; set; }
    [Required]
    public TimeSpan EndTime { get; set; }
    public string? CourseName { get; set; }
}

public class AddLabSectionsRequest
{
    [MinLength(1)]
    public List<LabSectionSlotDto> Sections { get; set; } = new();
}

public class LabDeviceDto
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public string Name { get; set; } = string.Empty;
    public LabDeviceType DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLabDeviceRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Device password used for token login (stored as hash).</summary>
    [Required, MinLength(6), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    public LabDeviceType DeviceType { get; set; } = LabDeviceType.Esp32Controller;

    [MaxLength(64)]
    public string? IpAddress { get; set; }
}

public class UpdateLabDeviceRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public LabDeviceType DeviceType { get; set; } = LabDeviceType.Esp32Controller;

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>If set, replaces the device password (re-hashed).</summary>
    [MinLength(6), MaxLength(128)]
    public string? NewPassword { get; set; }
}
