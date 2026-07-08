namespace Greenlab.Models;

public class LabDevice
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string DeviceKeyHash { get; set; } = string.Empty;
    public LabDeviceType DeviceType { get; set; } = LabDeviceType.Esp32Controller;
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LabSensor> Sensors { get; set; } = new List<LabSensor>();
}
