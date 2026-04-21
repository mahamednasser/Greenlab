namespace Greenlab.Models;

public class Lab
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Shared secret for ESP32 (header X-Lab-Device-Key).</summary>
    public string DeviceKey { get; set; } = string.Empty;
    public int LightThreshold { get; set; } = 2000;
    public float TempFanOnThresholdCelsius { get; set; } = 28f;
    public string? CvLabCode { get; set; }
    public string? CameraHlspUrl { get; set; }
    public int LastOccupancyCount { get; set; }
    public DateTime? LastOccupancyAt { get; set; }

    public LabControlOverride? ControlOverride { get; set; }
    public ICollection<LabSectionSchedule> SectionSchedules { get; set; } = new List<LabSectionSchedule>();
    public ICollection<StaffLabAssignment> StaffAssignments { get; set; } = new List<StaffLabAssignment>();
}
