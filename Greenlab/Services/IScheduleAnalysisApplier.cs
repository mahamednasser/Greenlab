using Greenlab.DTOs;

namespace Greenlab.Services;

public interface IScheduleAnalysisApplier
{
    Task<ApplyScheduleAnalysisResultDto> ApplyAsync(ApplyScheduleAnalysisRequest request, CancellationToken cancellationToken = default);
}
