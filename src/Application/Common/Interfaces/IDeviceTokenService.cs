// Layer: Application
// Responsibility: Defines the contract for issuing JWT access tokens for ESP32 devices.
namespace Greenlab.Application.Common.Interfaces;

public interface IDeviceTokenService
{
    string CreateToken(int deviceId, int labId, out DateTime expiresAtUtc);
}
