using System.Net.Http.Json;
using System.Text.Json;
using Greenlab.Models;
using Greenlab.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Greenlab.Services;

public class CrowdCountClient : ICrowdCountClient
{
    private readonly HttpClient _http;
    private readonly CrowdVisionOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CrowdCountClient> _logger;

    public CrowdCountClient(
        HttpClient http,
        IOptions<CrowdVisionOptions> options,
        IMemoryCache cache,
        ILogger<CrowdCountClient> logger)
    {
        _http = http;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> GetOccupancyCountAsync(Lab lab, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"occ:{lab.Id}";
        if (_cache.TryGetValue(cacheKey, out int cached))
            return cached;

        var count = await FetchCountAsync(lab, cancellationToken).ConfigureAwait(false);

        _cache.Set(cacheKey, count, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Clamp(_options.CacheSeconds, 1, 60))
        });

        return count;
    }

    private async Task<int> FetchCountAsync(Lab lab, CancellationToken cancellationToken)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return _options.UseStubWhenUnavailable ? _options.StubCount : 0;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/occupancy");
            req.Content = JsonContent.Create(new
            {
                lab_code = lab.CvLabCode ?? lab.Id.ToString(),
                camera_url = lab.CameraRtspUrl
            });

            if (!string.IsNullOrEmpty(_options.ApiKey))
                req.Headers.Add("X-Api-Key", _options.ApiKey);

            var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Crowd vision HTTP {Status}", resp.StatusCode);
                return _options.UseStubWhenUnavailable ? _options.StubCount : 0;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("count", out var c))
                return c.GetInt32();
            if (doc.RootElement.TryGetProperty("occupancy_count", out var o))
                return o.GetInt32();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Crowd vision call failed");
        }

        return _options.UseStubWhenUnavailable ? _options.StubCount : 0;
    }
}
