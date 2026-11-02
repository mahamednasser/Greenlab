// Layer: Application
// Responsibility: Contract for talking to a WiFi AC module over HTTP on the LAN.
namespace Greenlab.Application.Common.Interfaces;

public record SmartAcStatusResult(bool PowerOn, int? TargetTempC, bool Reachable);

public interface ISmartAcGatewayClient
{
    Task SetPowerAsync(string ipAddress, int port, string? pathPrefix, bool on, CancellationToken cancellationToken = default);
    Task SetTempAsync(string ipAddress, int port, string? pathPrefix, int celsius, CancellationToken cancellationToken = default);
    Task<SmartAcStatusResult> GetStatusAsync(string ipAddress, int port, string? pathPrefix, CancellationToken cancellationToken = default);
}
