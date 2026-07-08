// Layer: Application
// Responsibility: Handles device read use cases.
using Greenlab.Application.Common.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Devices.Queries;

public record GetLegacyLightStatusQuery : IRequest<string>;

public class GetLegacyLightStatusQueryHandler : IRequestHandler<GetLegacyLightStatusQuery, string>
{
    private readonly ILegacyDeviceStateService _state;

    public GetLegacyLightStatusQueryHandler(ILegacyDeviceStateService state)
    {
        _state = state;
    }

    public Task<string> Handle(GetLegacyLightStatusQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_state.GetLightStatus());
    }
}
