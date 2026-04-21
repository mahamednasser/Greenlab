namespace Greenlab.Models;

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

    public int? SetByUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
