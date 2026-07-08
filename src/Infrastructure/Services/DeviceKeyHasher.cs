// Layer: Infrastructure
// Responsibility: Hashes device keys using SHA-256.
using System.Security.Cryptography;
using System.Text;
using Greenlab.Application.Common.Interfaces;

namespace Greenlab.Infrastructure.Services;

public class DeviceKeyHasher : IDeviceKeyHasher
{
    public string Hash(string deviceKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(deviceKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
