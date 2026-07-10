// Layer: WebAPI
// Responsibility: Exposes staff endpoints and delegates use cases to MediatR.
using System.Security.Claims;
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.Labs.Commands;
using Greenlab.Application.Features.Staff.Commands;
using Greenlab.Application.Features.Staff.Queries;
using Greenlab.Domain.Interfaces;
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
    private readonly ILabRepository _labs;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly IHttpClientFactory _httpClientFactory;

    public StaffController(
        ISender sender,
        ILabRepository labs,
        IStaffLabAssignmentRepository assignments,
        IHttpClientFactory httpClientFactory)
    {
        _sender = sender;
        _labs = labs;
        _assignments = assignments;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("me/current-labs")]
    public async Task<ActionResult<IReadOnlyList<CurrentLabDto>>> CurrentLabs(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCurrentLabsQuery(GetUserId()), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("labs")]
    public async Task<ActionResult<IReadOnlyList<LabOptionDto>>> ListLabs(CancellationToken cancellationToken)
    {
        var labs = await _labs.ListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return Ok(labs
            .OrderBy(l => l.Name)
            .Select(l => new LabOptionDto { Id = l.Id, Name = l.Name })
            .ToList());
    }

    [HttpPost("labs/{labId:int}/override")]
    public async Task<IActionResult> SetOverride(int labId, [FromBody] SetLabOverrideRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetLabOverrideCommand(labId, GetUserId(), request, RequireStaffAccess: true, AuditAction: "StaffOverride"), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("labs/light/on")]
    public Task<IActionResult> TurnLightOn([FromQuery] int LabId, [FromQuery] int DeviceId = 1, CancellationToken cancellationToken = default)
        => ControlDeviceAsync(LabId, "light/on", onSuccess: () => LabDeviceStateCache.SetLightState(LabId, true), cancellationToken);

    [HttpPost("labs/light/off")]
    public Task<IActionResult> TurnLightOff([FromQuery] int LabId, [FromQuery] int DeviceId = 1, CancellationToken cancellationToken = default)
        => ControlDeviceAsync(LabId, "light/off", onSuccess: () => LabDeviceStateCache.SetLightState(LabId, false), cancellationToken);

    [HttpPost("labs/fan/on")]
    public Task<IActionResult> TurnFanOn([FromQuery] int LabId, [FromQuery] int DeviceId = 1, CancellationToken cancellationToken = default)
        => ControlDeviceAsync(LabId, "fan/on", onSuccess: () => LabDeviceStateCache.SetFanState(LabId, true), cancellationToken);

    [HttpPost("labs/fan/off")]
    public Task<IActionResult> TurnFanOff([FromQuery] int LabId, [FromQuery] int DeviceId = 1, CancellationToken cancellationToken = default)
        => ControlDeviceAsync(LabId, "fan/off", onSuccess: () => LabDeviceStateCache.SetFanState(LabId, false), cancellationToken);

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

    private async Task<IActionResult> ControlDeviceAsync(
        int labId,
        string path,
        Action onSuccess,
        CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(labId, cancellationToken).ConfigureAwait(false))
            return Forbid();

        var espIp = $"http://192.168.1.10/{path}";
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync(espIp, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "ESP32 returned failure response",
                    statusCode = response.StatusCode.ToString()
                });
            }

            onSuccess();
            return Ok(new { success = true, message = $"Command {path} sent successfully" });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { success = false, message = "ESP32 request timeout" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<bool> HasAccessAsync(int labId, CancellationToken cancellationToken)
    {
        var assignments = await _assignments.ListByUserAsync(GetUserId(), cancellationToken).ConfigureAwait(false);
        var allowed = StaffAccessEvaluator.GetAllowedLabIds(assignments, DateTime.UtcNow);
        return allowed.Contains(labId);
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
