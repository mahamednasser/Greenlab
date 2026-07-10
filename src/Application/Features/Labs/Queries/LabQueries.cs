// Layer: Application
// Responsibility: Handles lab read use cases.
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Labs.Queries;

public record GetLabsOverviewQuery : IRequest<IReadOnlyList<LabOverviewDto>>;

public record ListLabsQuery : IRequest<IReadOnlyList<LabDetailDto>>;

public record GetLabByIdQuery(int Id) : IRequest<LabDetailDto>;

public record ListLabDevicesQuery(int LabId) : IRequest<IReadOnlyList<LabDeviceDto>>;

public record GetLabDeviceByIdQuery(int LabId, int DeviceId) : IRequest<LabDeviceDto>;

public class GetLabsOverviewQueryHandler : IRequestHandler<GetLabsOverviewQuery, IReadOnlyList<LabOverviewDto>>
{
    private readonly ILabRepository _labs;

    public GetLabsOverviewQueryHandler(ILabRepository labs)
    {
        _labs = labs;
    }

    public async Task<IReadOnlyList<LabOverviewDto>> Handle(GetLabsOverviewQuery request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = ScheduleClock.ToLocal(utcNow);
        var timeOfDay = localNow.TimeOfDay;
        var day = localNow.DayOfWeek;
        var labs = await _labs.ListWithOverviewDataAsync(cancellationToken).ConfigureAwait(false);

        return labs.Select(l =>
        {
            var energy = LabDeviceStateCache.GetEnergy(l.Id, utcNow);
            var lightOn = l.ControlOverride?.ForceLightOn ?? energy.LightOn;
            var fanOn = l.ControlOverride?.ForceFanOn ?? energy.FanOn;
            if (lightOn != energy.LightOn || fanOn != energy.FanOn)
            {
                LabDeviceStateCache.SetState(l.Id, lightOn, fanOn);
                energy = LabDeviceStateCache.GetEnergy(l.Id, utcNow);
            }

            return new LabOverviewDto
            {
                Id = l.Id,
                Name = l.Name,
                LastOccupancyCount = l.LastOccupancyCount,
                LastOccupancyAt = l.LastOccupancyAt,
                InSession = l.SectionSchedules.Any(s => s.DayOfWeek == day && s.StartTime <= timeOfDay && s.EndTime >= timeOfDay),
                Override = LabMapping.ToOverrideDto(l.ControlOverride),
                ActualLightOn = lightOn,
                ActualFanOn = fanOn,
                LightRuntimeSeconds = energy.LightRuntimeSeconds,
                FanRuntimeSeconds = energy.FanRuntimeSeconds,
                CurrentWatts = energy.CurrentWatts,
                EnergyKwh = energy.EnergyKwh
            };
        }).ToList();
    }
}

public class ListLabsQueryHandler : IRequestHandler<ListLabsQuery, IReadOnlyList<LabDetailDto>>
{
    private readonly ILabRepository _labs;

    public ListLabsQueryHandler(ILabRepository labs)
    {
        _labs = labs;
    }

    public async Task<IReadOnlyList<LabDetailDto>> Handle(ListLabsQuery request, CancellationToken cancellationToken)
    {
        var labs = await _labs.ListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return labs.Select(LabMapping.ToDetailDto).ToList();
    }
}

public class GetLabByIdQueryHandler : IRequestHandler<GetLabByIdQuery, LabDetailDto>
{
    private readonly ILabRepository _labs;

    public GetLabByIdQueryHandler(ILabRepository labs)
    {
        _labs = labs;
    }

    public async Task<LabDetailDto> Handle(GetLabByIdQuery request, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Lab", request.Id);

        return LabMapping.ToDetailDto(lab);
    }
}

public class ListLabDevicesQueryHandler : IRequestHandler<ListLabDevicesQuery, IReadOnlyList<LabDeviceDto>>
{
    private readonly ILabRepository _labs;
    private readonly ILabDeviceRepository _devices;

    public ListLabDevicesQueryHandler(ILabRepository labs, ILabDeviceRepository devices)
    {
        _labs = labs;
        _devices = devices;
    }

    public async Task<IReadOnlyList<LabDeviceDto>> Handle(ListLabDevicesQuery request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        var devices = await _devices.ListByLabIdAsync(request.LabId, cancellationToken).ConfigureAwait(false);
        return devices.Select(LabMapping.ToDeviceDto).ToList();
    }
}

public class GetLabDeviceByIdQueryHandler : IRequestHandler<GetLabDeviceByIdQuery, LabDeviceDto>
{
    private readonly ILabRepository _labs;
    private readonly ILabDeviceRepository _devices;

    public GetLabDeviceByIdQueryHandler(ILabRepository labs, ILabDeviceRepository devices)
    {
        _labs = labs;
        _devices = devices;
    }

    public async Task<LabDeviceDto> Handle(GetLabDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        var device = await _devices.GetByLabAndDeviceIdNoTrackingAsync(request.LabId, request.DeviceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("LabDevice", request.DeviceId);

        return LabMapping.ToDeviceDto(device);
    }
}
