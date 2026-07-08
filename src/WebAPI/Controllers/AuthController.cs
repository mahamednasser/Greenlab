// Layer: WebAPI
// Responsibility: Exposes authentication endpoints and delegates login to MediatR.
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new LoginCommand(request), cancellationToken).ConfigureAwait(false);
        return response == null ? Unauthorized() : response;
    }
}
