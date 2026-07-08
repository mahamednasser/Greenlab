// Layer: Domain
// Responsibility: Defines the LabControlOverride domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

public class LabControlOverride
{
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    public bool MotionSensorDisabled { get; set; }

    /// <summary>null = automatic from sensors/occupancy; true/false = forced.</summary>
    public bool? ForceLightOn { get; set; }
    public bool? ForceFanOn { get; set; }
    public bool? ForceRelay3On { get; set; }
    public bool? ForceRelay4On { get; set; }
    public ControlMode ControlMode { get; set; } = ControlMode.Automatic;

    public int? SetByUserId { get; set; }
    public User? SetByUser { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
