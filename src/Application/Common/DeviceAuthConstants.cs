// Layer: Application
// Responsibility: Defines authentication constants for ESP32 device JWT authentication.
namespace Greenlab.Application.Common;

public static class DeviceAuthConstants
{
    public const string Scheme = "DeviceBearer";
    public const string Policy = "DeviceOnly";
    public const string DeviceIdClaim = "device_id";
    public const string LabIdClaim = "lab_id";
}
