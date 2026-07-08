// Layer: Application
// Responsibility: Handles device event and legacy device command use cases.
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Devices.Commands;

public record HandleDeviceEventCommand(int LabId, int AuthenticatedLabId, int AuthenticatedDeviceId, DeviceEventRequest Request) : IRequest<DeviceEventResponse?>;

public record SetLegacyLightStatusCommand(string Status) : IRequest;

public record HandleLegacyMotionCommand(string Ip) : IRequest<LegacyMotionResponse?>;

public record LegacyMotionResponse(bool Approved, string ForceLight);

public record IssueDeviceTokenCommand(int LabId, int DeviceId) : IRequest<DeviceTokenResponse>;
public record LoginDeviceForTokenCommand(int LabId, int DeviceId, string Password) : IRequest<DeviceTokenResponse?>;

public class HandleDeviceEventCommandHandler : IRequestHandler<HandleDeviceEventCommand, DeviceEventResponse?>
{
    private readonly ILabRepository _labs;
    private readonly ICrowdCountClient _crowd;
    private readonly IUnitOfWork _unitOfWork;

    public HandleDeviceEventCommandHandler(ILabRepository labs, ICrowdCountClient crowd, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _crowd = crowd;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeviceEventResponse?> Handle(HandleDeviceEventCommand request, CancellationToken cancellationToken)
    {
        if (request.LabId != request.AuthenticatedLabId)
            return null;

        var lab = await _labs.GetWithDeviceStateAsync(request.LabId, cancellationToken).ConfigureAwait(false);
        if (lab == null)
            return null;

        var device = lab.Devices.FirstOrDefault(d => d.IsActive && d.Id == request.AuthenticatedDeviceId);
        if (device == null)
            return null;

        device.LastSeenAt = DateTime.UtcNow;

        var controlOverride = lab.ControlOverride;
        if (controlOverride?.MotionSensorDisabled == true && request.Request.MotionDetected)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new DeviceEventResponse
            {
                LightOn = controlOverride.ForceLightOn ?? false,
                FanOn = controlOverride.ForceFanOn ?? false,
                Relay3On = controlOverride.ForceRelay3On ?? false,
                Relay4On = controlOverride.ForceRelay4On ?? false,
                MotionIgnored = true,
                OccupancyCount = lab.LastOccupancyCount
            };
        }

        var count = lab.LastOccupancyCount;
        if (request.Request.MotionDetected)
        {
            count = await _crowd.GetOccupancyCountAsync(lab, cancellationToken).ConfigureAwait(false);
            lab.LastOccupancyCount = count;
            lab.LastOccupancyAt = DateTime.UtcNow;
        }

        var lightAuto = false;
        var fanAuto = false;
        if (count > 0)
        {
            if (request.Request.LdrValue.HasValue && request.Request.LdrValue.Value < lab.LightThreshold)
                lightAuto = true;
            if (request.Request.TemperatureCelsius.HasValue && request.Request.TemperatureCelsius.Value >= lab.TempFanOnThresholdCelsius)
                fanAuto = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeviceEventResponse
        {
            LightOn = controlOverride?.ForceLightOn ?? lightAuto,
            FanOn = controlOverride?.ForceFanOn ?? fanAuto,
            Relay3On = controlOverride?.ForceRelay3On ?? false,
            Relay4On = controlOverride?.ForceRelay4On ?? false,
            MotionIgnored = false,
            OccupancyCount = count
        };
    }
}

public class IssueDeviceTokenCommandHandler : IRequestHandler<IssueDeviceTokenCommand, DeviceTokenResponse>
{
    private readonly ILabRepository _labs;
    private readonly IDeviceTokenService _tokens;

    public IssueDeviceTokenCommandHandler(ILabRepository labs, IDeviceTokenService tokens)
    {
        _labs = labs;
        _tokens = tokens;
    }

    public async Task<DeviceTokenResponse> Handle(IssueDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetWithDeviceStateAsync(request.LabId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Lab", request.LabId);

        var device = lab.Devices.FirstOrDefault(d => d.Id == request.DeviceId && d.IsActive);
        if (device == null)
            throw new NotFoundException("LabDevice", request.DeviceId);

        var token = _tokens.CreateToken(device.Id, lab.Id, out var expiresAt);
        return new DeviceTokenResponse
        {
            LabId = lab.Id,
            DeviceId = device.Id,
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}

public class LoginDeviceForTokenCommandHandler : IRequestHandler<LoginDeviceForTokenCommand, DeviceTokenResponse?>
{
    private readonly ILabRepository _labs;
    private readonly IDeviceKeyHasher _keyHasher;
    private readonly IDeviceTokenService _tokens;
    private readonly IUnitOfWork _unitOfWork;

    public LoginDeviceForTokenCommandHandler(ILabRepository labs, IDeviceKeyHasher keyHasher, IDeviceTokenService tokens, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _keyHasher = keyHasher;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeviceTokenResponse?> Handle(LoginDeviceForTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return null;

        var lab = await _labs.GetWithDeviceStateAsync(request.LabId, cancellationToken).ConfigureAwait(false);
        if (lab == null)
            return null;

        var device = lab.Devices.FirstOrDefault(d => d.Id == request.DeviceId && d.IsActive);
        if (device == null)
            return null;

        var passwordHash = _keyHasher.Hash(request.Password.Trim());
        if (!string.Equals(device.DeviceKeyHash, passwordHash, StringComparison.Ordinal))
            return null;

        var token = _tokens.CreateToken(device.Id, lab.Id, out var expiresAt);
        device.LastSeenAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new DeviceTokenResponse
        {
            LabId = lab.Id,
            DeviceId = device.Id,
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}

public class SetLegacyLightStatusCommandHandler : IRequestHandler<SetLegacyLightStatusCommand>
{
    private readonly ILegacyDeviceStateService _state;

    public SetLegacyLightStatusCommandHandler(ILegacyDeviceStateService state)
    {
        _state = state;
    }

    public Task Handle(SetLegacyLightStatusCommand request, CancellationToken cancellationToken)
    {
        _state.SetLightStatus(request.Status);
        return Task.CompletedTask;
    }
}

public class HandleLegacyMotionCommandHandler : IRequestHandler<HandleLegacyMotionCommand, LegacyMotionResponse?>
{
    private readonly ILegacyDeviceStateService _state;

    public HandleLegacyMotionCommandHandler(ILegacyDeviceStateService state)
    {
        _state = state;
    }

    public Task<LegacyMotionResponse?> Handle(HandleLegacyMotionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Ip))
            return Task.FromResult<LegacyMotionResponse?>(null);

        _state.SetLightStatus("on");
        return Task.FromResult<LegacyMotionResponse?>(new LegacyMotionResponse(true, "on"));
    }
}
