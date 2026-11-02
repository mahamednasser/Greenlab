// Layer: Application
// Responsibility: Handles smart WiFi AC CRUD and control commands.
using FluentValidation;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.SmartAcs.Commands;

public record CreateSmartAcCommand(CreateSmartAcRequest Request) : IRequest<SmartAcDto>;
public record UpdateSmartAcCommand(int AcId, UpdateSmartAcRequest Request) : IRequest<SmartAcDto>;
public record DeleteSmartAcCommand(int AcId) : IRequest;
public record SetSmartAcPowerCommand(int AcId, bool On, int UserId, bool RequireStaffAccess) : IRequest<SmartAcDto>;
public record SetSmartAcTempCommand(int AcId, int Celsius, int UserId, bool RequireStaffAccess) : IRequest<SmartAcDto>;

public class CreateSmartAcCommandValidator : AbstractValidator<CreateSmartAcCommand>
{
    public CreateSmartAcCommandValidator()
    {
        RuleFor(x => x.Request.LabId).GreaterThan(0);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.IpAddress).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Request.PathPrefix).MaximumLength(100);
    }
}

public class UpdateSmartAcCommandValidator : AbstractValidator<UpdateSmartAcCommand>
{
    public UpdateSmartAcCommandValidator()
    {
        RuleFor(x => x.Request.LabId).GreaterThan(0);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.IpAddress).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Request.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Request.PathPrefix).MaximumLength(100);
    }
}

public class SetSmartAcTempCommandValidator : AbstractValidator<SetSmartAcTempCommand>
{
    public SetSmartAcTempCommandValidator()
    {
        RuleFor(x => x.Celsius).InclusiveBetween(16, 30);
    }
}

public class CreateSmartAcCommandHandler : IRequestHandler<CreateSmartAcCommand, SmartAcDto>
{
    private readonly ILabRepository _labs;
    private readonly ISmartAcRepository _acs;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSmartAcCommandHandler(ILabRepository labs, ISmartAcRepository acs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _acs = acs;
        _unitOfWork = unitOfWork;
    }

    public async Task<SmartAcDto> Handle(CreateSmartAcCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.Request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.Request.LabId);

        var entity = new SmartAc
        {
            LabId = request.Request.LabId,
            Name = request.Request.Name.Trim(),
            IpAddress = request.Request.IpAddress.Trim(),
            Port = request.Request.Port,
            PathPrefix = string.IsNullOrWhiteSpace(request.Request.PathPrefix) ? null : request.Request.PathPrefix.Trim(),
            IsActive = true
        };

        await _acs.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var withLab = await _acs.GetByIdWithLabAsync(entity.Id, cancellationToken).ConfigureAwait(false);
        return SmartAcMapping.ToDto(withLab!);
    }
}

public class UpdateSmartAcCommandHandler : IRequestHandler<UpdateSmartAcCommand, SmartAcDto>
{
    private readonly ILabRepository _labs;
    private readonly ISmartAcRepository _acs;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSmartAcCommandHandler(ILabRepository labs, ISmartAcRepository acs, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _acs = acs;
        _unitOfWork = unitOfWork;
    }

    public async Task<SmartAcDto> Handle(UpdateSmartAcCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.Request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.Request.LabId);

        var entity = await _acs.GetByIdAsync(request.AcId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("SmartAc", request.AcId);

        entity.LabId = request.Request.LabId;
        entity.Name = request.Request.Name.Trim();
        entity.IpAddress = request.Request.IpAddress.Trim();
        entity.Port = request.Request.Port;
        entity.PathPrefix = string.IsNullOrWhiteSpace(request.Request.PathPrefix) ? null : request.Request.PathPrefix.Trim();
        entity.IsActive = request.Request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var withLab = await _acs.GetByIdWithLabAsync(entity.Id, cancellationToken).ConfigureAwait(false);
        return SmartAcMapping.ToDto(withLab!);
    }
}

public class DeleteSmartAcCommandHandler : IRequestHandler<DeleteSmartAcCommand>
{
    private readonly ISmartAcRepository _acs;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSmartAcCommandHandler(ISmartAcRepository acs, IUnitOfWork unitOfWork)
    {
        _acs = acs;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSmartAcCommand request, CancellationToken cancellationToken)
    {
        var entity = await _acs.GetByIdAsync(request.AcId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("SmartAc", request.AcId);
        _acs.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public class SetSmartAcPowerCommandHandler : IRequestHandler<SetSmartAcPowerCommand, SmartAcDto>
{
    private readonly ISmartAcRepository _acs;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly ISmartAcGatewayClient _gateway;
    private readonly IUnitOfWork _unitOfWork;

    public SetSmartAcPowerCommandHandler(
        ISmartAcRepository acs,
        IStaffLabAssignmentRepository assignments,
        ISmartAcGatewayClient gateway,
        IUnitOfWork unitOfWork)
    {
        _acs = acs;
        _assignments = assignments;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<SmartAcDto> Handle(SetSmartAcPowerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _acs.GetByIdWithLabAsync(request.AcId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("SmartAc", request.AcId);

        if (!entity.IsActive)
            throw new DomainException("This smart AC is inactive.");

        if (request.RequireStaffAccess)
            await EnsureStaffAccessAsync(request.UserId, entity.LabId, cancellationToken).ConfigureAwait(false);

        try
        {
            await _gateway.SetPowerAsync(entity.IpAddress, entity.Port, entity.PathPrefix, request.On, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new DomainException($"Could not reach smart AC at {entity.IpAddress}: {ex.Message}");
        }

        entity.LastPowerOn = request.On;
        entity.LastSeenAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return SmartAcMapping.ToDto(entity);
    }

    private async Task EnsureStaffAccessAsync(int userId, int labId, CancellationToken cancellationToken)
    {
        var assignments = await _assignments.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var allowed = Application.Common.StaffAccessEvaluator.GetAllowedLabIds(assignments, DateTime.UtcNow);
        if (!allowed.Contains(labId))
            throw new ForbiddenAccessException("The current staff user cannot access this lab at this time.");
    }
}

public class SetSmartAcTempCommandHandler : IRequestHandler<SetSmartAcTempCommand, SmartAcDto>
{
    private readonly ISmartAcRepository _acs;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly ISmartAcGatewayClient _gateway;
    private readonly IUnitOfWork _unitOfWork;

    public SetSmartAcTempCommandHandler(
        ISmartAcRepository acs,
        IStaffLabAssignmentRepository assignments,
        ISmartAcGatewayClient gateway,
        IUnitOfWork unitOfWork)
    {
        _acs = acs;
        _assignments = assignments;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<SmartAcDto> Handle(SetSmartAcTempCommand request, CancellationToken cancellationToken)
    {
        var entity = await _acs.GetByIdWithLabAsync(request.AcId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("SmartAc", request.AcId);

        if (!entity.IsActive)
            throw new DomainException("This smart AC is inactive.");

        if (request.RequireStaffAccess)
        {
            var assignments = await _assignments.ListByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            var allowed = Application.Common.StaffAccessEvaluator.GetAllowedLabIds(assignments, DateTime.UtcNow);
            if (!allowed.Contains(entity.LabId))
                throw new ForbiddenAccessException("The current staff user cannot access this lab at this time.");
        }

        try
        {
            await _gateway.SetTempAsync(entity.IpAddress, entity.Port, entity.PathPrefix, request.Celsius, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new DomainException($"Could not reach smart AC at {entity.IpAddress}: {ex.Message}");
        }

        entity.LastTargetTempC = request.Celsius;
        entity.LastSeenAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return SmartAcMapping.ToDto(entity);
    }
}

public static class SmartAcMapping
{
    public static SmartAcDto ToDto(SmartAc a) => new()
    {
        Id = a.Id,
        LabId = a.LabId,
        LabName = a.Lab?.Name ?? string.Empty,
        Name = a.Name,
        IpAddress = a.IpAddress,
        Port = a.Port,
        PathPrefix = a.PathPrefix,
        IsActive = a.IsActive,
        LastTargetTempC = a.LastTargetTempC,
        LastPowerOn = a.LastPowerOn,
        LastSeenAt = a.LastSeenAt,
        CreatedAt = a.CreatedAt
    };
}
