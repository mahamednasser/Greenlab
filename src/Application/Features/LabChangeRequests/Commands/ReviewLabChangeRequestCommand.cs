// Layer: Application
// Responsibility: Handles admin review of lab change requests.
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.LabChangeRequests.Commands;

public record ReviewLabChangeRequestCommand(int Id, int AdminUserId, ReviewLabChangeRequest Request) : IRequest;

public class ReviewLabChangeRequestCommandHandler : IRequestHandler<ReviewLabChangeRequestCommand>
{
    private readonly ILabChangeRequestRepository _requests;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewLabChangeRequestCommandHandler(
        ILabChangeRequestRepository requests,
        IStaffLabAssignmentRepository assignments,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _assignments = assignments;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReviewLabChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await _requests.GetForReviewAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("LabChangeRequest", request.Id);

        if (changeRequest.Status != LabChangeRequestStatus.Pending)
            throw new DomainException("Already reviewed.");

        changeRequest.ReviewedByUserId = request.AdminUserId;
        changeRequest.ReviewedAt = DateTime.UtcNow;
        changeRequest.ReviewNote = request.Request.Note;

        if (request.Request.Approve)
        {
            changeRequest.Status = LabChangeRequestStatus.Approved;
            await _assignments.AddAsync(new StaffLabAssignment
            {
                UserId = changeRequest.RequestedByUserId,
                LabId = changeRequest.ToLabId,
                ValidFrom = changeRequest.SectionStart,
                ValidUntil = changeRequest.SectionEnd,
                AssignmentSource = StaffAssignmentSource.AdminApprovedTransfer
            }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            changeRequest.Status = LabChangeRequestStatus.Rejected;
        }

        await _auditLogs.AddAsync(new AuditLog
        {
            Action = request.Request.Approve ? "LabChangeApproved" : "LabChangeRejected",
            UserId = request.AdminUserId,
            Details = $"Request {request.Id}"
        }, cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
