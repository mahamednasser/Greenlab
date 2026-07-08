// Layer: Application
// Responsibility: Defines the contract for hashing device keys.
namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Hashes device keys for secure comparison.
/// </summary>
public interface IDeviceKeyHasher
{
    /// <summary>
    /// Hashes the supplied device key.
    /// </summary>
    string Hash(string deviceKey);
}
