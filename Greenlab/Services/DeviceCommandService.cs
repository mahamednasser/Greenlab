using Greenlab.Data;
using Greenlab.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Services;

public class DeviceCommandService : IDeviceCommandService
{
    private readonly AppDbContext _db;
    private readonly ICrowdCountClient _crowd;

    public DeviceCommandService(AppDbContext db, ICrowdCountClient crowd)
    {
        _db = db;
        _crowd = crowd;
    }

    public async Task<DeviceEventResponse?> HandleDeviceEventAsync(int labId, string? deviceKey, DeviceEventRequest request, CancellationToken cancellationToken = default)
    {
        var lab = await _db.Labs
            .Include(l => l.ControlOverride)
            .Include(l => l.Devices)
            .FirstOrDefaultAsync(l => l.Id == labId, cancellationToken).ConfigureAwait(false);

        if (lab == null || string.IsNullOrEmpty(deviceKey))
            return null;

        var device = lab.Devices.FirstOrDefault(d =>
            d.IsActive
            && string.Equals(d.DeviceKeyHash, DeviceKeyHasher.Hash(deviceKey), StringComparison.Ordinal));
        var legacyKeyMatches = string.Equals(lab.DeviceKey, deviceKey, StringComparison.Ordinal);
        if (device == null && !legacyKeyMatches)
            return null;

        if (device != null)
            device.LastSeenAt = DateTime.UtcNow;

        var ov = lab.ControlOverride;

        if (ov?.MotionSensorDisabled == true && request.MotionDetected)
        {
            return new DeviceEventResponse
            {
                LightOn = ov.ForceLightOn ?? false,
                FanOn = ov.ForceFanOn ?? false,
                Relay3On = ov.ForceRelay3On ?? false,
                Relay4On = ov.ForceRelay4On ?? false,
                MotionIgnored = true,
                OccupancyCount = lab.LastOccupancyCount
            };
        }

        var count = lab.LastOccupancyCount;
        if (request.MotionDetected)
        {
            count = await _crowd.GetOccupancyCountAsync(lab, cancellationToken).ConfigureAwait(false);
            lab.LastOccupancyCount = count;
            lab.LastOccupancyAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        bool lightAuto = false, fanAuto = false;
        if (count > 0)
        {
            if (request.LdrValue.HasValue && request.LdrValue.Value < lab.LightThreshold)
                lightAuto = true;
            if (request.TemperatureCelsius.HasValue && request.TemperatureCelsius.Value >= lab.TempFanOnThresholdCelsius)
                fanAuto = true;
        }

        var light = ov?.ForceLightOn ?? lightAuto;
        var fan = ov?.ForceFanOn ?? fanAuto;
        var r3 = ov?.ForceRelay3On ?? false;
        var r4 = ov?.ForceRelay4On ?? false;

        return new DeviceEventResponse
        {
            LightOn = light,
            FanOn = fan,
            Relay3On = r3,
            Relay4On = r4,
            MotionIgnored = false,
            OccupancyCount = count
        };
    }
}
