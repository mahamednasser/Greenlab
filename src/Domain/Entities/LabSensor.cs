// Layer: Domain
// Responsibility: Defines the LabSensor domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

public class LabSensor
{
    public int Id { get; set; }
    public int LabDeviceId { get; set; }
    public LabDevice LabDevice { get; set; } = null!;
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public LabSensorType SensorType { get; set; }
    public string? Unit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
