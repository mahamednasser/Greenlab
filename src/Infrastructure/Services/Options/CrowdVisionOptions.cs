// Layer: Infrastructure
// Responsibility: Defines configuration options for CrowdVisionOptions infrastructure services.
namespace Greenlab.Infrastructure.Services.Options;

public class CrowdVisionOptions
{
    public const string SectionName = "CrowdVision";
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string? ApiKey { get; set; }
    /// <summary>When true or when HTTP fails, use this count for development.</summary>
    public bool UseStubWhenUnavailable { get; set; } = true;
    public int StubCount { get; set; } = 1;
    public int CacheSeconds { get; set; } = 3;
}
