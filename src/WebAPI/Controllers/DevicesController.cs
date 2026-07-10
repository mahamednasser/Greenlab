// Layer: WebAPI
// Responsibility: Exposes device endpoints and delegates command handling to MediatR.
using Greenlab.Application.Common;
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.Devices.Commands;
using Greenlab.Application.Features.Devices.Queries;
using Greenlab.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = DeviceAuthConstants.Scheme, Policy = DeviceAuthConstants.Policy)]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILabRepository _labs;

    public DevicesController(ISender sender , IHttpClientFactory httpClientFactory, ILabRepository labs)
    {
        _sender = sender;
        _httpClientFactory = httpClientFactory;
        _labs = labs;
    }

    [HttpPost("{labId:int}/event")]
    public async Task<ActionResult<DeviceEventResponse>> PostEvent(
        int labId,
        [FromBody] DeviceEventRequest body,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedDevice(out var authenticatedLabId, out var authenticatedDeviceId))
            return Unauthorized();

        var lab = await _labs.GetWithDeviceStateAsync(labId, cancellationToken).ConfigureAwait(false);
        if (lab == null)
            return NotFound("Lab not found");

        var url = !string.IsNullOrWhiteSpace(lab.CameraRtspUrl) ? lab.CameraRtspUrl : "http://localhost:8000/current-count";

        var client=_httpClientFactory.CreateClient();
        var responsee = await client.GetAsync(url);

        if ( !responsee.IsSuccessStatusCode )
            return StatusCode((int)responsee.StatusCode , "Python API error");

        var content = await responsee.Content.ReadAsStringAsync();

        var data = await responsee.Content.ReadFromJsonAsync<PeopleCountResponse>();
        if(data!=null && data.PeopleCount > 0 )
        {
          var response = await _sender.Send(
            new HandleDeviceEventCommand(labId, authenticatedLabId, authenticatedDeviceId, body),
            cancellationToken).ConfigureAwait(false);

            response.OccupancyCount = data.PeopleCount;

             return response == null ? Unauthorized() : response;
        }

        return Ok(new DeviceEventResponse
        {
            LightOn = false,
            FanOn = false,
            Relay3On = false,
            Relay4On = false,
            MotionIgnored = true,
            OccupancyCount = 0
        });

    }





    public class PeopleCountResponse
    {
        [JsonPropertyName("people_count")]
        public int PeopleCount { get; set; }
    }





    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<DeviceTokenResponse>> TokenLogin([FromBody] DeviceTokenLoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new LoginDeviceForTokenCommand(request.LabId, request.DeviceId, request.Password),
            cancellationToken).ConfigureAwait(false);

        return response == null ? Unauthorized() : Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] MotionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new HandleLegacyMotionCommand(request.Ip), cancellationToken).ConfigureAwait(false);
        return response == null ? BadRequest() : Ok(new MotionResponse { Approved = response.Approved, ForceLight = response.ForceLight });
    }

    [HttpGet("light")]
    public async Task<IActionResult> GetLight(CancellationToken cancellationToken)
    {
        var status = await _sender.Send(new GetLegacyLightStatusQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(new { status });
    }

  




    public class MotionRequest
    {
        public string Ip { get; set; } = string.Empty;
    }

    public class MotionResponse
    {
        public bool Approved { get; set; }
        public string ForceLight { get; set; } = "auto";
    }

    private bool TryGetAuthenticatedDevice(out int labId, out int deviceId)
    {
        labId = 0;
        deviceId = 0;

        var labClaim = User.FindFirstValue(DeviceAuthConstants.LabIdClaim);
        var deviceClaim = User.FindFirstValue(DeviceAuthConstants.DeviceIdClaim);
        return int.TryParse(labClaim, out labId) && int.TryParse(deviceClaim, out deviceId);
    }
}
