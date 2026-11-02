// Layer: Infrastructure
// Responsibility: HTTP client for generic WiFi AC modules (ESP/gateway style endpoints).
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Greenlab.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlab.Infrastructure.Services;

/// <summary>
/// Talks to AC WiFi cards / ESP bridges that expose:
/// GET {base}/power/on|off, GET {base}/temp?c=24, GET {base}/status
/// </summary>
public class SmartAcGatewayClient : ISmartAcGatewayClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmartAcGatewayClient> _logger;

    public SmartAcGatewayClient(IHttpClientFactory httpClientFactory, ILogger<SmartAcGatewayClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task SetPowerAsync(string ipAddress, int port, string? pathPrefix, bool on, CancellationToken cancellationToken = default)
    {
        var path = on ? "power/on" : "power/off";
        return SendAsync(ipAddress, port, pathPrefix, path, cancellationToken);
    }

    public Task SetTempAsync(string ipAddress, int port, string? pathPrefix, int celsius, CancellationToken cancellationToken = default)
        => SendAsync(ipAddress, port, pathPrefix, $"temp?c={celsius}", cancellationToken);

    public async Task<SmartAcStatusResult> GetStatusAsync(
        string ipAddress,
        int port,
        string? pathPrefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient();
            var url = BuildUrl(ipAddress, port, pathPrefix, "status");
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Smart AC status HTTP {Status} from {Url}", (int)response.StatusCode, url);
                return new SmartAcStatusResult(false, null, false);
            }

            var body = await response.Content.ReadFromJsonAsync<StatusDto>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (body == null)
                return new SmartAcStatusResult(false, null, true);

            return new SmartAcStatusResult(body.PowerOn, body.TargetTempC ?? body.TempC, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Smart AC status unreachable at {Ip}:{Port}", ipAddress, port);
            return new SmartAcStatusResult(false, null, false);
        }
    }

    private async Task SendAsync(string ipAddress, int port, string? pathPrefix, string relativePath, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var url = BuildUrl(ipAddress, port, pathPrefix, relativePath);
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Smart AC at {ipAddress}:{port} returned {(int)response.StatusCode}: {body}");
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("SmartAc");
        client.Timeout = TimeSpan.FromSeconds(5);
        return client;
    }

    internal static string BuildUrl(string ipAddress, int port, string? pathPrefix, string relativePath)
    {
        var host = ipAddress.Trim().TrimEnd('/');
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Allow full base URL in IpAddress for flexibility.
            var prefix = NormalizePrefix(pathPrefix);
            return $"{host.TrimEnd('/')}{prefix}/{relativePath.TrimStart('/')}";
        }

        var prefixPath = NormalizePrefix(pathPrefix);
        return $"http://{host}:{port}{prefixPath}/{relativePath.TrimStart('/')}";
    }

    private static string NormalizePrefix(string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return string.Empty;
        var p = pathPrefix.Trim();
        if (!p.StartsWith('/'))
            p = "/" + p;
        return p.TrimEnd('/');
    }

    private sealed class StatusDto
    {
        [JsonPropertyName("powerOn")]
        public bool PowerOn { get; set; }

        [JsonPropertyName("targetTempC")]
        public int? TargetTempC { get; set; }

        [JsonPropertyName("tempC")]
        public int? TempC { get; set; }

        [JsonPropertyName("power")]
        public bool? Power { get; set; }
    }
}
