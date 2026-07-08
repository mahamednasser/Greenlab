// Layer: WebAPI
// Responsibility: Exposes staff endpoints and delegates use cases to MediatR.
using System.Security.Claims;
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.Labs.Commands;
using Greenlab.Application.Features.Staff.Commands;
using Greenlab.Application.Features.Staff.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/staff")]
public class StaffController : ControllerBase
{
    private readonly ISender _sender;

    public StaffController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me/current-labs")]
    public async Task<ActionResult<IReadOnlyList<CurrentLabDto>>> CurrentLabs(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCurrentLabsQuery(GetUserId()), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("labs/{labId:int}/override")]
    public async Task<IActionResult> SetOverride(int labId, [FromBody] SetLabOverrideRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetLabOverrideCommand(labId, GetUserId(), request, RequireStaffAccess: true, AuditAction: "StaffOverride"), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("lab-change-requests")]
    public async Task<ActionResult<int>> CreateLabChangeRequest([FromBody] CreateLabChangeRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new CreateLabChangeRequestCommand(GetUserId(), request), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("lab-change-requests/mine")]
    public async Task<ActionResult<IReadOnlyList<LabChangeRequestDto>>> MyLabChangeRequests(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetMyLabChangeRequestsQuery(GetUserId()), cancellationToken).ConfigureAwait(false));
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
