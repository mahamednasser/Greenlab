// Layer: Application
// Responsibility: Handles staff read use cases.
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Staff.Queries;

public record CurrentLabDto(
    int Id,
    string Name,
    bool InSession,
    LabOverrideDto? Override,
    bool ActualLightOn = false,
    bool ActualFanOn = false,
    double LightRuntimeSeconds = 0,
    double FanRuntimeSeconds = 0,
    double CurrentWatts = 0,
    double EnergyKwh = 0);

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
        var utcNow = DateTime.UtcNow;
        var localNow = ScheduleClock.ToLocal(utcNow);
        var assignmentRows = await _assignments.ListByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        var labIds = StaffAccessEvaluator.GetAllowedLabIds(assignmentRows, utcNow);
        if (labIds.Count == 0)
            return new List<CurrentLabDto>();

        var timeOfDay = localNow.TimeOfDay;
        var day = localNow.DayOfWeek;
        var labs = await _labs.ListCurrentForStaffAsync(labIds, cancellationToken).ConfigureAwait(false);
        return labs.Select(l => {
            var energy = LabDeviceStateCache.GetEnergy(l.Id, utcNow);
            var lightOn = l.ControlOverride?.ForceLightOn ?? energy.LightOn;
            var fanOn = l.ControlOverride?.ForceFanOn ?? energy.FanOn;
            // Keep cache aligned if override forces a different state without going through Set*State.
            if (lightOn != energy.LightOn || fanOn != energy.FanOn)
            {
                LabDeviceStateCache.SetState(l.Id, lightOn, fanOn);
                energy = LabDeviceStateCache.GetEnergy(l.Id, utcNow);
            }
            return new CurrentLabDto(
                l.Id,
                l.Name,
                l.SectionSchedules.Any(s => s.DayOfWeek == day && s.StartTime <= timeOfDay && s.EndTime >= timeOfDay),
                LabMapping.ToOverrideDto(l.ControlOverride),
                lightOn,
                fanOn,
                energy.LightRuntimeSeconds,
                energy.FanRuntimeSeconds,
                energy.CurrentWatts,
                energy.EnergyKwh);
        }).ToList();
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
