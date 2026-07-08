// Layer: Application
// Responsibility: Defines request and response DTOs for DeviceDtos use cases.
namespace Greenlab.Application.DTOs;

public class DeviceEventRequest
{
    public bool MotionDetected { get; set; }
    public int? DistanceCm { get; set; }
    public int? LdrValue { get; set; }
    public float? TemperatureCelsius { get; set; }
    public float? Humidity { get; set; }
}

public class DeviceEventResponse
{
    public bool LightOn { get; set; }
    public bool FanOn { get; set; }
    public bool Relay3On { get; set; }
    public bool Relay4On { get; set; }
    public bool MotionIgnored { get; set; }
    public int OccupancyCount { get; set; }
}

public class DeviceTokenResponse
{
    public int LabId { get; set; }
    public int DeviceId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class DeviceTokenLoginRequest
{
    public int LabId { get; set; }
    public int DeviceId { get; set; }
    public string Password { get; set; } = string.Empty;
}
