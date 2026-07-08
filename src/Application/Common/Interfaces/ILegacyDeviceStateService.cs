// Layer: Application
// Responsibility: Defines the contract for preserving legacy device light state endpoints.
namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Stores legacy light state used by existing simple device endpoints.
/// </summary>
public interface ILegacyDeviceStateService
{
    /// <summary>
    /// Gets the current light status.
    /// </summary>
    string GetLightStatus();

    /// <summary>
    /// Sets the current light status.
    /// </summary>
    void SetLightStatus(string status);
}
