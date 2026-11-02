// Layer: WebAPI
// Responsibility: Admin CRUD and control endpoints for smart WiFi ACs.
using System.Security.Claims;
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.SmartAcs.Commands;
using Greenlab.Application.Features.SmartAcs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Route("api/admin/smart-acs")]
[Authorize(Policy = "AdminOnly")]
public class AdminSmartAcsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminSmartAcsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SmartAcDto>>> List(CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListSmartAcsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("by-lab/{labId:int}")]
    public async Task<ActionResult<IReadOnlyList<SmartAcDto>>> ListByLab(int labId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListSmartAcsByLabQuery(labId), cancellationToken).ConfigureAwait(false));

    [HttpGet("{acId:int}")]
    public async Task<ActionResult<SmartAcDto>> Get(int acId, CancellationToken cancellationToken)
    {
        var row = await _sender.Send(new GetSmartAcQuery(acId), cancellationToken).ConfigureAwait(false);
        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<SmartAcDto>> Create([FromBody] CreateSmartAcRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreateSmartAcCommand(request), cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { acId = created.Id }, created);
    }

    [HttpPut("{acId:int}")]
    public async Task<ActionResult<SmartAcDto>> Update(int acId, [FromBody] UpdateSmartAcRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new UpdateSmartAcCommand(acId, request), cancellationToken).ConfigureAwait(false));

    [HttpDelete("{acId:int}")]
    public async Task<IActionResult> Delete(int acId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteSmartAcCommand(acId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{acId:int}/power/on")]
    public async Task<ActionResult<SmartAcDto>> PowerOn(int acId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SetSmartAcPowerCommand(acId, true, GetUserId(), false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{acId:int}/power/off")]
    public async Task<ActionResult<SmartAcDto>> PowerOff(int acId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SetSmartAcPowerCommand(acId, false, GetUserId(), false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{acId:int}/temp")]
    public async Task<ActionResult<SmartAcDto>> SetTemp(int acId, [FromBody] SetSmartAcTempRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SetSmartAcTempCommand(acId, request.Celsius, GetUserId(), false), cancellationToken).ConfigureAwait(false));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Route("api/staff/smart-acs")]
[Authorize(Policy = "StaffOnly")]
public class StaffSmartAcsController : ControllerBase
{
    private readonly ISender _sender;

    public StaffSmartAcsController(ISender sender) => _sender = sender;

    [HttpGet("by-lab/{labId:int}")]
    public async Task<ActionResult<IReadOnlyList<SmartAcDto>>> ListByLab(int labId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ListSmartAcsByLabQuery(labId), cancellationToken).ConfigureAwait(false));

    [HttpPost("{acId:int}/power/on")]
    public async Task<ActionResult<SmartAcDto>> PowerOn(int acId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SetSmartAcPowerCommand(acId, true, GetUserId(), true), cancellationToken).ConfigureAwait(false));

    [HttpPost("{acId:int}/power/off")]
    public async Task<ActionResult<SmartAcDto>> PowerOff(int acId, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SetSmartAcPowerCommand(acId, false, GetUserId(), true), cancellationToken).ConfigureAwait(false));

    [HttpPost("{acId:int}/temp")]
    public async Task<ActionResult<SmartAcDto>> SetTemp(int acId, [FromBody] SetSmartAcTempRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new SetSmartAcTempCommand(acId, request.Celsius, GetUserId(), true), cancellationToken).ConfigureAwait(false));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
