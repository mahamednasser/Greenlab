// Layer: Application
// Responsibility: Handles lab change request read use cases.
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.LabChangeRequests.Queries;

public record ListLabChangeRequestsQuery(LabChangeRequestStatus? Status) : IRequest<IReadOnlyList<LabChangeRequestDto>>;

public class ListLabChangeRequestsQueryHandler : IRequestHandler<ListLabChangeRequestsQuery, IReadOnlyList<LabChangeRequestDto>>
{
    private readonly ILabChangeRequestRepository _requests;

    public ListLabChangeRequestsQueryHandler(ILabChangeRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<IReadOnlyList<LabChangeRequestDto>> Handle(ListLabChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _requests.ListAsync(request.Status, cancellationToken).ConfigureAwait(false);
        return rows.Select(LabMapping.ToLabChangeRequestDto).ToList();
    }
}
