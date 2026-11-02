// Layer: Application
// Responsibility: Queries for smart WiFi ACs.
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.SmartAcs.Commands;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.SmartAcs.Queries;

public record ListSmartAcsQuery : IRequest<IReadOnlyList<SmartAcDto>>;
public record ListSmartAcsByLabQuery(int LabId) : IRequest<IReadOnlyList<SmartAcDto>>;
public record GetSmartAcQuery(int AcId) : IRequest<SmartAcDto?>;

public class ListSmartAcsQueryHandler : IRequestHandler<ListSmartAcsQuery, IReadOnlyList<SmartAcDto>>
{
    private readonly ISmartAcRepository _acs;

    public ListSmartAcsQueryHandler(ISmartAcRepository acs) => _acs = acs;

    public async Task<IReadOnlyList<SmartAcDto>> Handle(ListSmartAcsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _acs.ListAllAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(SmartAcMapping.ToDto).ToList();
    }
}

public class ListSmartAcsByLabQueryHandler : IRequestHandler<ListSmartAcsByLabQuery, IReadOnlyList<SmartAcDto>>
{
    private readonly ISmartAcRepository _acs;

    public ListSmartAcsByLabQueryHandler(ISmartAcRepository acs) => _acs = acs;

    public async Task<IReadOnlyList<SmartAcDto>> Handle(ListSmartAcsByLabQuery request, CancellationToken cancellationToken)
    {
        var rows = await _acs.ListByLabIdAsync(request.LabId, cancellationToken).ConfigureAwait(false);
        return rows.Select(a =>
        {
            var dto = SmartAcMapping.ToDto(a);
            return dto;
        }).ToList();
    }
}

public class GetSmartAcQueryHandler : IRequestHandler<GetSmartAcQuery, SmartAcDto?>
{
    private readonly ISmartAcRepository _acs;

    public GetSmartAcQueryHandler(ISmartAcRepository acs) => _acs = acs;

    public async Task<SmartAcDto?> Handle(GetSmartAcQuery request, CancellationToken cancellationToken)
    {
        var row = await _acs.GetByIdWithLabAsync(request.AcId, cancellationToken).ConfigureAwait(false);
        return row == null ? null : SmartAcMapping.ToDto(row);
    }
}
