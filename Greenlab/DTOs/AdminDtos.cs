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
