// Layer: Application
// Responsibility: Admin CRUD for lab devices (ESP32).
using FluentValidation;
using Greenlab.Application.Common;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Labs.Commands;

public record CreateLabDeviceCommand(int LabId, CreateLabDeviceRequest Request) : IRequest<LabDeviceDto>;

public record UpdateLabDeviceCommand(int LabId, int DeviceId, UpdateLabDeviceRequest Request) : IRequest<LabDeviceDto>;

public record DeleteLabDeviceCommand(int LabId, int DeviceId) : IRequest;

public class CreateLabDeviceCommandValidator : AbstractValidator<CreateLabDeviceCommand>
{
    public CreateLabDeviceCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.Request.IpAddress).MaximumLength(64);
    }
}

public class UpdateLabDeviceCommandValidator : AbstractValidator<UpdateLabDeviceCommand>
{
    public UpdateLabDeviceCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.IpAddress).MaximumLength(64);
        RuleFor(x => x.Request.NewPassword).MinimumLength(6).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.Request.NewPassword));
    }
}

public class CreateLabDeviceCommandHandler : IRequestHandler<CreateLabDeviceCommand, LabDeviceDto>
{
    private readonly ILabRepository _labs;
    private readonly ILabDeviceRepository _devices;
    private readonly IDeviceKeyHasher _keyHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLabDeviceCommandHandler(
        ILabRepository labs,
        ILabDeviceRepository devices,
        IDeviceKeyHasher keyHasher,
        IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _devices = devices;
        _keyHasher = keyHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabDeviceDto> Handle(CreateLabDeviceCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        var device = new LabDevice
        {
            LabId = request.LabId,
            Name = request.Request.Name.Trim(),
            DeviceKeyHash = _keyHasher.Hash(request.Request.Password.Trim()),
            DeviceType = request.Request.DeviceType,
            IpAddress = string.IsNullOrWhiteSpace(request.Request.IpAddress) ? null : request.Request.IpAddress.Trim(),
            IsActive = true
        };

        await _devices.AddAsync(device, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return LabMapping.ToDeviceDto(device);
    }
}

public class UpdateLabDeviceCommandHandler : IRequestHandler<UpdateLabDeviceCommand, LabDeviceDto>
{
    private readonly ILabRepository _labs;
    private readonly ILabDeviceRepository _devices;
    private readonly IDeviceKeyHasher _keyHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLabDeviceCommandHandler(
        ILabRepository labs,
        ILabDeviceRepository devices,
        IDeviceKeyHasher keyHasher,
        IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _devices = devices;
        _keyHasher = keyHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabDeviceDto> Handle(UpdateLabDeviceCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        var device = await _devices.GetByLabAndDeviceIdAsync(request.LabId, request.DeviceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("LabDevice", request.DeviceId);

        device.Name = request.Request.Name.Trim();
        device.DeviceType = request.Request.DeviceType;
        device.IpAddress = string.IsNullOrWhiteSpace(request.Request.IpAddress) ? null : request.Request.IpAddress.Trim();
        device.IsActive = request.Request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Request.NewPassword))
            device.DeviceKeyHash = _keyHasher.Hash(request.Request.NewPassword.Trim());

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return LabMapping.ToDeviceDto(device);
    }
}

public class DeleteLabDeviceCommandHandler : IRequestHandler<DeleteLabDeviceCommand>
{
    private readonly ILabRepository _labs;
    private readonly ILabDeviceRepository _devices;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLabDeviceCommandHandler(ILabRepository labs, ILabDeviceRepository devices, IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _devices = devices;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteLabDeviceCommand request, CancellationToken cancellationToken)
    {
        if (!await _labs.ExistsAsync(request.LabId, cancellationToken).ConfigureAwait(false))
            throw new NotFoundException("Lab", request.LabId);

        var device = await _devices.GetByLabAndDeviceIdAsync(request.LabId, request.DeviceId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("LabDevice", request.DeviceId);

        _devices.Remove(device);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
