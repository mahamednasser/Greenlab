// Layer: Application
// Responsibility: Handles staff read use cases.
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Staff.Queries;

public record CurrentLabDto(int Id, string Name, bool InSession, LabOverrideDto? Override);

public record GetCurrentLabsQuery(int UserId) : IRequest<IReadOnlyList<CurrentLabDto>>;

public record GetMyLabChangeRequestsQuery(int UserId) : IRequest<IReadOnlyList<LabChangeRequestDto>>;

public class GetCurrentLabsQueryHandler : IRequestHandler<GetCurrentLabsQuery, IReadOnlyList<CurrentLabDto>>
{
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly ILabRepository _labs;

    public GetCurrentLabsQueryHandler(IStaffLabAssignmentRepository assignments, ILabRepository labs)
    {
        _assignments = assignments;
        _labs = labs;
    }

    public async Task<IReadOnlyList<CurrentLabDto>> Handle(GetCurrentLabsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var assignmentRows = await _assignments.ListByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        var labIds = StaffAccessEvaluator.GetAllowedLabIds(assignmentRows, now);
        if (labIds.Count == 0)
            return new List<CurrentLabDto>();

        var timeOfDay = now.TimeOfDay;
        var day = now.DayOfWeek;
        var labs = await _labs.ListCurrentForStaffAsync(labIds, cancellationToken).ConfigureAwait(false);
        return labs.Select(l => new CurrentLabDto(
            l.Id,
            l.Name,
            l.SectionSchedules.Any(s => s.DayOfWeek == day && s.StartTime <= timeOfDay && s.EndTime >= timeOfDay),
            LabMapping.ToOverrideDto(l.ControlOverride))).ToList();
    }
}

public class GetMyLabChangeRequestsQueryHandler : IRequestHandler<GetMyLabChangeRequestsQuery, IReadOnlyList<LabChangeRequestDto>>
{
    private readonly ILabChangeRequestRepository _requests;

    public GetMyLabChangeRequestsQueryHandler(ILabChangeRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<IReadOnlyList<LabChangeRequestDto>> Handle(GetMyLabChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _requests.ListByRequesterAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        return rows.Select(LabMapping.ToLabChangeRequestDto).ToList();
    }
}
