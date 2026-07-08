// Layer: Infrastructure
// Responsibility: Preserves legacy in-memory light status used by older device endpoints.
using Greenlab.Application.Common.Interfaces;

namespace Greenlab.Infrastructure.Services;

public class LegacyDeviceStateService : ILegacyDeviceStateService
{
    private string _lightStatus = "off";

    public string GetLightStatus()
    {
        return _lightStatus;
    }

    public void SetLightStatus(string status)
    {
        _lightStatus = status;
    }
}
