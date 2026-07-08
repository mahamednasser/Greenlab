using Greenlab.DTOs;
using Greenlab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Greenlab.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceCommandService _device;

    public DevicesController(IDeviceCommandService device) => _device = device;

    /// <summary>ESP32: motion + optional sensor readings. Header X-Lab-Device-Key required.</summary>
    [HttpPost("{labId:int}/event")]
    public async Task<ActionResult<DeviceEventResponse>> PostEvent(
        int labId,
        [FromHeader(Name = "X-Lab-Device-Key")] string? deviceKey,
        [FromBody] DeviceEventRequest body,
        CancellationToken cancellationToken)
    {
        var res = await _device.HandleDeviceEventAsync(labId, deviceKey, body, cancellationToken).ConfigureAwait(false);
        if (res == null)
            return Unauthorized();
        return res;
    }






    [HttpPost]
    public IActionResult Post( [FromBody] MotionRequest request )
    {
        if ( string.IsNullOrEmpty(request.Ip) )
            return BadRequest();

        var response = new MotionResponse
        {
            Approved = true ,
            ForceLight = GetLightMode(request.Ip)
        };

        return Ok(response);
    }

    private string GetLightMode( string ip )
    {
        // تقدر تتحكم هنا
        // من DB أو UI
        DeviceState.LightStatus = "on";
        return "on";
        // "on"
        // "off"
    }




    public static class DeviceState
    {
        public static string LightStatus = "off"; // on / off / auto
    }



    [HttpGet("light")]
    public IActionResult GetLight()
    {
        return Ok(new
        {
            status = DeviceState.LightStatus
        });
    }

    // 🔘 تشغيل النور
    [HttpPost("light/on")]
    public IActionResult TurnOn()
    {
        DeviceState.LightStatus = "on";
        return Ok("Light ON");
    }

    // 🔘 إطفاء النور
    [HttpPost("light/off")]
    public IActionResult TurnOff()
    {
        DeviceState.LightStatus = "off";
        return Ok("Light OFF");
    }











    public class MotionRequest
    {
        public string Ip { get; set; } = string.Empty;
    }
    public class MotionResponse
    {
        public bool Approved { get; set; }
        public string ForceLight { get; set; } = "auto"; // on / off / auto
    }




}
