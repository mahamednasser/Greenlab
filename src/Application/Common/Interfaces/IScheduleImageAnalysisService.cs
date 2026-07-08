// Layer: Application
// Responsibility: Defines the contract for AI schedule image analysis.
using Greenlab.Application.DTOs;

namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Extracts schedule data from uploaded timetable images.
/// </summary>
public interface IScheduleImageAnalysisService
{
    /// <summary>
    /// Analyzes an image and returns proposed schedule data.
    /// </summary>
    Task<ScheduleAnalysisResultDto> AnalyzeImageAsync(byte[] imageBytes, string imageMimeType, CancellationToken cancellationToken = default);
}
