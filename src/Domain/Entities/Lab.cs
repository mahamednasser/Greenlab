// Layer: Domain
// Responsibility: Defines the Lab domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

public class Lab
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Shared secret for ESP32 (header X-Lab-Device-Key).</summary>
    public string DeviceKey { get; set; } = string.Empty;
    public int LightThreshold { get; set; } = 2000;
    public float TempFanOnThresholdCelsius { get; set; } = 28f;
    public string? CvLabCode { get; set; }
    public string? CameraRtspUrl { get; set; }
    public int LastOccupancyCount { get; set; }
    public DateTime? LastOccupancyAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LabControlOverride? ControlOverride { get; set; }
    public ICollection<LabDevice> Devices { get; set; } = new List<LabDevice>();
    public ICollection<SmartAc> SmartAcs { get; set; } = new List<SmartAc>();
    public ICollection<LabSensor> Sensors { get; set; } = new List<LabSensor>();
    public ICollection<LabSectionSchedule> SectionSchedules { get; set; } = new List<LabSectionSchedule>();
    public ICollection<StaffLabAssignment> StaffAssignments { get; set; } = new List<StaffLabAssignment>();
    public ICollection<ManualControlCommand> ManualControlCommands { get; set; } = new List<ManualControlCommand>();
}
