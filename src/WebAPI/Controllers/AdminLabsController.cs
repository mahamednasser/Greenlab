// Layer: WebAPI
// Responsibility: Exposes admin lab management endpoints and delegates use cases to MediatR.
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.Devices.Commands;
using Greenlab.Application.Features.Labs.Commands;
using Greenlab.Application.Features.Labs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/labs")]
public class AdminLabsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHttpClientFactory _httpClientFactory;


    public AdminLabsController(ISender sender , IHttpClientFactory httpClientFactory )
    {
        _sender = sender;
        _httpClientFactory = httpClientFactory;

    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LabDetailDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new ListLabsQuery(), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LabDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetLabByIdQuery(id), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost]
    public async Task<ActionResult<LabDetailDto>> Create([FromBody] CreateLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateLabCommand(request), cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LabDetailDto>> Update(int id, [FromBody] UpdateLabRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new UpdateLabCommand(id, request), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteLabCommand(id), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:int}/sections")]
    public async Task<IActionResult> AddSections(int id, [FromBody] AddLabSectionsRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new AddLabSectionsCommand(id, request), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("{labId:int}/devices")]
    public async Task<ActionResult<IReadOnlyList<LabDeviceDto>>> ListDevices(int labId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new ListLabDevicesQuery(labId), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{labId:int}/devices/{deviceId:int}")]
    public async Task<ActionResult<LabDeviceDto>> GetDevice(int labId, int deviceId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetLabDeviceByIdQuery(labId, deviceId), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{labId:int}/devices")]
    public async Task<ActionResult<LabDeviceDto>> CreateDevice(int labId, [FromBody] CreateLabDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateLabDeviceCommand(labId, request), cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetDevice), new { labId, deviceId = result.Id }, result);
    }

    [HttpPut("{labId:int}/devices/{deviceId:int}")]
    public async Task<ActionResult<LabDeviceDto>> UpdateDevice(int labId, int deviceId, [FromBody] UpdateLabDeviceRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new UpdateLabDeviceCommand(labId, deviceId, request), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("{labId:int}/devices/{deviceId:int}")]
    public async Task<IActionResult> DeleteDevice(int labId, int deviceId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteLabDeviceCommand(labId, deviceId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{labId:int}/devices/{deviceId:int}/token")]
    public async Task<ActionResult<DeviceTokenResponse>> IssueDeviceToken(int labId, int deviceId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new IssueDeviceTokenCommand(labId, deviceId), cancellationToken).ConfigureAwait(false));
    }
















    [HttpPost("light/on")]
    public async Task<IActionResult> TurnLightOn( int LabId , int DeviceId )
    {
        var espIp = "http://192.168.1.10/light/on";

        try
        {
            var client = _httpClientFactory.CreateClient();

            client.Timeout = TimeSpan.FromSeconds(5); // ??? ???? ???? TaskCanceledException

            var response = await client.GetAsync(espIp);

            if ( response.IsSuccessStatusCode )
            {
                return Ok(new
                {
                    success = true ,
                    message = "Light turned ON successfully on ESP32"
                });
            }

            return StatusCode(500 , new
            {
                success = false ,
                message = "ESP32 returned failure response" ,
                statusCode = response.StatusCode.ToString()
            });
        }
        catch ( TaskCanceledException )
        {
            return StatusCode(504 , new
            {
                success = false ,
                message = "ESP32 request timeout"
            });
        }
        catch ( Exception ex )
        {
            return StatusCode(500 , new
            {
                success = false ,
                error = ex.Message
            });
        }
    }

    [HttpPost("light/off")]
    public async Task<IActionResult> TurnLightOff( int LabId , int DeviceId )
    {
        // IP ???? ???? ??? ESP32
        var espIp = "http://192.168.1.10/light/off";

        try
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(espIp);

            if ( response.IsSuccessStatusCode )
            {
                return Ok(new
                {
                    success = true ,
                    message = "Light turned OFF successfully on ESP32"
                });
            }

            return StatusCode(500 , new
            {
                success = false ,
                message = "Failed to reach ESP32"
            });
        }
        catch ( Exception ex )
        {
            return StatusCode(500 , new
            {
                success = false ,
                error = ex.Message
            });
        }
    }







    [HttpPost("fan/on")]
    public async Task<IActionResult> TurnFanOn( int LabId , int DeviceId )
    {
        var espIp = "http://192.168.1.10/fan/on";

        try
        {
            var client = _httpClientFactory.CreateClient();

            client.Timeout = TimeSpan.FromSeconds(5); // ??? ???? ???? TaskCanceledException

            var response = await client.GetAsync(espIp);

            if ( response.IsSuccessStatusCode )
            {
                return Ok(new
                {
                    success = true ,
                    message = "Fan turned ON successfully on ESP32"
                });
            }

            return StatusCode(500 , new
            {
                success = false ,
                message = "ESP32 returned failure response" ,
                statusCode = response.StatusCode.ToString()
            });
        }
        catch ( TaskCanceledException )
        {
            return StatusCode(504 , new
            {
                success = false ,
                message = "ESP32 request timeout"
            });
        }
        catch ( Exception ex )
        {
            return StatusCode(500 , new
            {
                success = false ,
                error = ex.Message
            });
        }
    }

    [HttpPost("fan/off")]
    public async Task<IActionResult> TurnfanOff( int LabId , int DeviceId )
    {
        // IP ???? ???? ??? ESP32
        var espIp = "http://192.168.1.10/fan/off";

        try
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(espIp);

            if ( response.IsSuccessStatusCode )
            {
                return Ok(new
                {
                    success = true ,
                    message = "Fan turned OFF successfully on ESP32"
                });
            }

            return StatusCode(500 , new
            {
                success = false ,
                message = "Failed to reach ESP32"
            });
        }
        catch ( Exception ex )
        {
            return StatusCode(500 , new
            {
                success = false ,
                error = ex.Message
            });
        }
    }














}
