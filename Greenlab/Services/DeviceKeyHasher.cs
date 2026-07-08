using System.Security.Cryptography;
using System.Text;

namespace Greenlab.Services;

public static class DeviceKeyHasher
{
    public static string Hash(string deviceKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(deviceKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
