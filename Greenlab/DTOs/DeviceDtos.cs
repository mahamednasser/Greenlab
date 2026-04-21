namespace Greenlab.DTOs;

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
