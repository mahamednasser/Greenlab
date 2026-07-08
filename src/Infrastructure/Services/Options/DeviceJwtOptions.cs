// Layer: Infrastructure
// Responsibility: Defines configuration options for ESP32 device JWT authentication.
namespace Greenlab.Infrastructure.Services.Options;

public class DeviceJwtOptions
{
    public const string SectionName = "DeviceJwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 60;
    public int ClockSkewSeconds { get; set; } = 60;
}
