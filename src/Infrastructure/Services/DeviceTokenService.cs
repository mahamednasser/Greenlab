// Layer: Infrastructure
// Responsibility: Creates JWT tokens for ESP32 devices.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Greenlab.Application.Common;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Infrastructure.Services.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Greenlab.Infrastructure.Services;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly DeviceJwtOptions _options;

    public DeviceTokenService(IOptions<DeviceJwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(int deviceId, int labId, out DateTime expiresAtUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, deviceId.ToString()),
            new(DeviceAuthConstants.DeviceIdClaim, deviceId.ToString()),
            new(DeviceAuthConstants.LabIdClaim, labId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
