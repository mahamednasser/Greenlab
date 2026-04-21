using Greenlab.DTOs;

namespace Greenlab.Services;

public interface IDeviceCommandService
{
    Task<DeviceEventResponse?> HandleDeviceEventAsync(int labId, string? deviceKey, DeviceEventRequest request, CancellationToken cancellationToken = default);
}
