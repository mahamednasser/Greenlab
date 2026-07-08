// Layer: Domain
// Responsibility: Defines the LabDeviceType domain enumeration.
namespace Greenlab.Domain.Enums;

public enum LabDeviceType
{
    Esp32Controller = 0,
    MotionSensor = 1,
    TemperatureSensor = 2,
    HumiditySensor = 3,
    LightSensor = 4,
    RelayModule = 5,
    Camera = 6
}
