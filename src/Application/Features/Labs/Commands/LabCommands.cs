// Layer: Application
// Responsibility: Handles lab mutation use cases.
using System.Text.Json;
using FluentValidation;
using Greenlab.Application.Common;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Labs.Commands;

public record CreateLabCommand(CreateLabRequest Request) : IRequest<LabDetailDto>;

public record UpdateLabCommand(int Id, UpdateLabRequest Request) : IRequest<LabDetailDto>;

public record AddLabSectionsCommand(int LabId, AddLabSectionsRequest Request) : IRequest;

public record DeleteLabCommand(int Id) : IRequest;

public record SetLabOverrideCommand(int LabId, int UserId, SetLabOverrideRequest Request, bool RequireStaffAccess, string AuditAction) : IRequest;

public class CreateLabCommandValidator : AbstractValidator<CreateLabCommand>
{
    public CreateLabCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.DeviceKey).MaximumLength(128);
    }
}

public class UpdateLabCommandValidator : AbstractValidator<UpdateLabCommand>
{
    public UpdateLabCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.DeviceKey).NotEmpty().MaximumLength(128);
    }
}

public class CreateLabCommandHandler : IRequestHandler<CreateLabCommand, LabDetailDto>
{
    private readonly ILabRepository _labs;
    private readonly ILabControlOverrideRepository _overrides;
    private readonly ILabDeviceRepository _devices;
    private readonly IDeviceKeyHasher _keyHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLabCommandHandler(
        ILabRepository labs,
        ILabControlOverrideRepository overrides,
        ILabDeviceRepository devices,
        IDeviceKeyHasher keyHasher,
        IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _overrides = overrides;
        _devices = devices;
        _keyHasher = keyHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabDetailDto> Handle(CreateLabCommand request, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(request.Request.DeviceKey) ? Guid.NewGuid().ToString("N") : request.Request.DeviceKey.Trim();
        var lab = new Lab
        {
            Name = request.Request.Name.Trim(),
            DeviceKey = key,
            LightThreshold = request.Request.LightThreshold,
            TempFanOnThresholdCelsius = request.Request.TempFanOnThresholdCelsius,
            CvLabCode = string.IsNullOrWhiteSpace(request.Request.CvLabCode) ? null : request.Request.CvLabCode.Trim(),
            CameraRtspUrl = string.IsNullOrWhiteSpace(request.Request.CameraRtspUrl) ? null : request.Request.CameraRtspUrl.Trim()
        };

        await _labs.AddAsync(lab, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _overrides.AddAsync(new LabControlOverride { LabId = lab.Id, UpdatedAt = DateTime.UtcNow }, cancellationToken).ConfigureAwait(false);
        await _devices.AddAsync(new LabDevice
        {
            LabId = lab.Id,
            Name = $"{lab.Name} ESP32",
            DeviceKeyHash = _keyHasher.Hash(key),
            DeviceType = LabDeviceType.Esp32Controller
        }, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return LabMapping.ToDetailDto(lab);
    }
}

public class UpdateLabCommandHandler : IRequestHandler<UpdateLabCommand, LabDetailDto>
{
    private readonly ILabRepository _labs;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLabCommandHandler(ILabRepository labs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabDetailDto> Handle(UpdateLabCommand request, CancellationToken cancellationToken)
    {
        var lab = await _labs.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Lab", request.Id);

        lab.Name = request.Request.Name.Trim();
        lab.DeviceKey = request.Request.DeviceKey.Trim();
        lab.LightThreshold = request.Request.LightThreshold;
        lab.TempFanOnThresholdCelsius = request.Request.TempFanOnThresholdCelsius;
        lab.CvLabCode = string.IsNullOrWhiteSpace(request.Request.CvLabCode) ? null : request.Request.CvLabCode.Trim();
        lab.CameraRtspUrl = string.IsNullOrWhiteSpace(request.Request.CameraRtspUrl) ? null : request.Request.CameraRtspUrl.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return LabMapping.ToDetailDto(lab);
    }
}

public class AddLabSectionsCommandHandler : IRequestHandler<AddLabSectionsCommand>
{
    private readonly ILabRepository _labs;
    private readonly ILabSectionScheduleRepository _sections;
    private readonly IUnitOfWork _unitOfWork;

    public AddLabSectionsCommandHandler(ILabRepository labs, ILabSectionScheduleRepository sections, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _sections = sections;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddLabSectionsCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        foreach (var section in request.Request.Sections)
        {
            await _sections.AddAsync(new LabSectionSchedule
            {
                LabId = request.LabId,
                DayOfWeek = section.DayOfWeek,
                StartTime = section.StartTime,
                EndTime = section.EndTime,
                CourseName = section.CourseName
            }, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public class DeleteLabCommandHandler : IRequestHandler<DeleteLabCommand>
{
    private readonly ILabRepository _labs;

    public DeleteLabCommandHandler(ILabRepository labs)
    {
        _labs = labs;
    }

    public async Task Handle(DeleteLabCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.Id, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.Id);

        await _labs.DeleteCascadeAsync(request.Id, cancellationToken).ConfigureAwait(false);
    }
}

public class SetLabOverrideCommandHandler : IRequestHandler<SetLabOverrideCommand>
{
    private readonly ILabRepository _labs;
    private readonly ILabControlOverrideRepository _overrides;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly IManualControlCommandRepository _commands;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;

    public SetLabOverrideCommandHandler(
        ILabRepository labs,
        ILabControlOverrideRepository overrides,
        IStaffLabAssignmentRepository assignments,
        IManualControlCommandRepository commands,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _overrides = overrides;
        _assignments = assignments;
        _commands = commands;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetLabOverrideCommand request, CancellationToken cancellationToken)
    {
        if (request.RequireStaffAccess)
        {
            var assignments = await _assignments.ListByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            var allowedLabIds = StaffAccessEvaluator.GetAllowedLabIds(assignments, DateTime.UtcNow);
            if (!allowedLabIds.Contains(request.LabId))
                throw new ForbiddenAccessException("The current staff user cannot access this lab at this time.");
        }

        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        var controlOverride = await _overrides.GetByLabIdAsync(request.LabId, cancellationToken).ConfigureAwait(false);
        if (controlOverride == null)
        {
            controlOverride = new LabControlOverride { LabId = request.LabId };
            await _overrides.AddAsync(controlOverride, cancellationToken).ConfigureAwait(false);
        }

        ApplyOverride(controlOverride, request.Request, request.UserId);
        await AddManualCommandsAsync(request.LabId, request.UserId, request.Request, cancellationToken).ConfigureAwait(false);
        await _auditLogs.AddAsync(new AuditLog
        {
            Action = request.AuditAction,
            LabId = request.LabId,
            UserId = request.UserId,
            Details = JsonSerializer.Serialize(request.Request)
        }, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ApplyOverride(LabControlOverride controlOverride, SetLabOverrideRequest request, int userId)
    {
        if (request.MotionSensorDisabled.HasValue) controlOverride.MotionSensorDisabled = request.MotionSensorDisabled.Value;
        if (request.ForceLightOn.HasValue) controlOverride.ForceLightOn = request.ForceLightOn;
        if (request.ForceFanOn.HasValue) controlOverride.ForceFanOn = request.ForceFanOn;
        if (request.ForceRelay3On.HasValue) controlOverride.ForceRelay3On = request.ForceRelay3On;
        if (request.ForceRelay4On.HasValue) controlOverride.ForceRelay4On = request.ForceRelay4On;

        controlOverride.SetByUserId = userId;
        controlOverride.ControlMode = HasManualOverride(request) ? ControlMode.Manual : ControlMode.Automatic;
        controlOverride.UpdatedAt = DateTime.UtcNow;
    }

    private async Task AddManualCommandsAsync(int labId, int userId, SetLabOverrideRequest request, CancellationToken cancellationToken)
    {
        if (request.MotionSensorDisabled.HasValue)
            await AddManualCommandAsync(labId, userId, ManualControlTarget.MotionSensor, request.MotionSensorDisabled.Value.ToString(), cancellationToken).ConfigureAwait(false);
        if (request.ForceLightOn.HasValue)
            await AddManualCommandAsync(labId, userId, ManualControlTarget.Light, request.ForceLightOn.Value.ToString(), cancellationToken).ConfigureAwait(false);
        if (request.ForceFanOn.HasValue)
            await AddManualCommandAsync(labId, userId, ManualControlTarget.Fan, request.ForceFanOn.Value.ToString(), cancellationToken).ConfigureAwait(false);
        if (request.ForceRelay3On.HasValue)
            await AddManualCommandAsync(labId, userId, ManualControlTarget.Relay3, request.ForceRelay3On.Value.ToString(), cancellationToken).ConfigureAwait(false);
        if (request.ForceRelay4On.HasValue)
            await AddManualCommandAsync(labId, userId, ManualControlTarget.Relay4, request.ForceRelay4On.Value.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private async Task AddManualCommandAsync(int labId, int userId, ManualControlTarget target, string value, CancellationToken cancellationToken)
    {
        await _commands.AddAsync(new ManualControlCommand
        {
            LabId = labId,
            RequestedByUserId = userId,
            Target = target,
            RequestedValue = value,
            CommandStatus = ManualControlCommandStatus.Sent
        }, cancellationToken).ConfigureAwait(false);
    }

    private static bool HasManualOverride(SetLabOverrideRequest request) =>
        request.MotionSensorDisabled == true
        || request.ForceLightOn.HasValue
        || request.ForceFanOn.HasValue
        || request.ForceRelay3On.HasValue
        || request.ForceRelay4On.HasValue;
}
