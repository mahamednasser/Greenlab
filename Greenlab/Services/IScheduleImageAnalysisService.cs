using Greenlab.DTOs;

namespace Greenlab.Services;

public interface IScheduleImageAnalysisService
{
    /// <summary>Uses configured vision model (OpenAI) to extract labs, sections, and staff rows.</summary>
    Task<ScheduleAnalysisResultDto> AnalyzeImageAsync(byte[] imageBytes, string imageMimeType, CancellationToken cancellationToken = default);
}
