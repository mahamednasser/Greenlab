// Layer: Application
// Responsibility: Handles staff requests to change labs.
using FluentValidation;
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Staff.Commands;

public record CreateLabChangeRequestCommand(int UserId, CreateLabChangeRequestDto Request) : IRequest<int>;

public class CreateLabChangeRequestCommandValidator : AbstractValidator<CreateLabChangeRequestCommand>
{
    public CreateLabChangeRequestCommandValidator()
    {
        RuleFor(x => x.Request.SectionEnd).GreaterThan(x => x.Request.SectionStart);
    }
}

public class CreateLabChangeRequestCommandHandler : IRequestHandler<CreateLabChangeRequestCommand, int>
{
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly ILabRepository _labs;
    private readonly ILabSectionScheduleRepository _sections;
    private readonly ILabChangeRequestRepository _requests;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLabChangeRequestCommandHandler(
        IStaffLabAssignmentRepository assignments,
        ILabRepository labs,
        ILabSectionScheduleRepository sections,
        ILabChangeRequestRepository requests,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork)
    {
        _assignments = assignments;
        _labs = labs;
        _sections = sections;
        _requests = requests;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateLabChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var assignmentRows = await _assignments.ListByUserAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        var allowedLabIds = StaffAccessEvaluator.GetAllowedLabIds(assignmentRows, DateTime.UtcNow);
        if (!allowedLabIds.Contains(request.Request.FromLabId))
            throw new ForbiddenAccessException("The current staff user cannot request a change from this lab at this time.");

        if (!await _labs.ExistsAsync(request.Request.ToLabId, cancellationToken).ConfigureAwait(false))
            throw new DomainException("Unknown ToLabId.");

        var autoApprove = !await _sections.HasOverlapAsync(
            request.Request.ToLabId,
            request.Request.SectionStart,
            request.Request.SectionEnd,
            cancellationToken).ConfigureAwait(false);

        var changeRequest = new LabChangeRequest
        {
            RequestedByUserId = request.UserId,
            FromLabId = request.Request.FromLabId,
            ToLabId = request.Request.ToLabId,
            SectionStart = request.Request.SectionStart,
            SectionEnd = request.Request.SectionEnd,
            Status = autoApprove ? LabChangeRequestStatus.AutoApproved : LabChangeRequestStatus.Pending,
            ReviewedAt = autoApprove ? DateTime.UtcNow : null,
            ReviewNote = autoApprove ? "Auto-approved because target lab is free." : null
        };
        await _requests.AddAsync(changeRequest, cancellationToken).ConfigureAwait(false);

        if (autoApprove)
        {
            await _assignments.AddAsync(new StaffLabAssignment
            {
                UserId = request.UserId,
                LabId = request.Request.ToLabId,
                ValidFrom = request.Request.SectionStart,
                ValidUntil = request.Request.SectionEnd,
                AssignmentSource = StaffAssignmentSource.AutoApprovedTransfer
            }, cancellationToken).ConfigureAwait(false);

            await _auditLogs.AddAsync(new AuditLog
            {
                Action = "LabChangeAutoApproved",
                UserId = request.UserId,
                LabId = request.Request.ToLabId,
                Details = $"From lab {request.Request.FromLabId}; {request.Request.SectionStart:o} - {request.Request.SectionEnd:o}"
            }, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return changeRequest.Id;
    }
}
