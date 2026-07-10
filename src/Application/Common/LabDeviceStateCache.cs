using System.Collections.Concurrent;

namespace Greenlab.Application.Common;

/// <summary>
/// In-memory device ON/OFF state plus accumulated runtime for energy estimates.
/// Light = 20 W, fan = 70 W (used by the API when exposing energy kWh).
/// </summary>
public static class LabDeviceStateCache
{
    public const double LightWatts = 20;
    public const double FanWatts = 70;

    private static readonly ConcurrentDictionary<int, LabPowerState> States = new();

    public static void SetState(int labId, bool lightOn, bool fanOn)
    {
        States.AddOrUpdate(
            labId,
            _ => CreateInitial(lightOn, fanOn),
            (_, old) => ApplyBoth(old, lightOn, fanOn, DateTime.UtcNow));
    }

    public static void SetLightState(int labId, bool lightOn)
    {
        States.AddOrUpdate(
            labId,
            _ => CreateInitial(lightOn, false),
            (_, old) => ApplyLight(old, lightOn, DateTime.UtcNow));
    }

    public static void SetFanState(int labId, bool fanOn)
    {
        States.AddOrUpdate(
            labId,
            _ => CreateInitial(false, fanOn),
            (_, old) => ApplyFan(old, fanOn, DateTime.UtcNow));
    }

    public static (bool LightOn, bool FanOn) GetState(int labId)
    {
        if (!States.TryGetValue(labId, out var state))
            return (false, false);
        return (state.LightOn, state.FanOn);
    }

    /// <summary>Runtime totals including any currently-ON interval up to <paramref name="utcNow"/>.</summary>
    public static LabEnergySnapshot GetEnergy(int labId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (!States.TryGetValue(labId, out var state))
            return LabEnergySnapshot.Empty;

        var lightSec = state.LightOnSeconds;
        var fanSec = state.FanOnSeconds;
        if (state.LightOn && state.LightOnSince.HasValue)
            lightSec += Math.Max(0, (now - state.LightOnSince.Value).TotalSeconds);
        if (state.FanOn && state.FanOnSince.HasValue)
            fanSec += Math.Max(0, (now - state.FanOnSince.Value).TotalSeconds);

        var watts = (state.LightOn ? LightWatts : 0) + (state.FanOn ? FanWatts : 0);
        var kwh = (lightSec / 3600.0) * (LightWatts / 1000.0)
                + (fanSec / 3600.0) * (FanWatts / 1000.0);

        return new LabEnergySnapshot(
            state.LightOn,
            state.FanOn,
            lightSec,
            fanSec,
            watts,
            Math.Round(kwh, 4));
    }

    private static LabPowerState CreateInitial(bool lightOn, bool fanOn)
    {
        var now = DateTime.UtcNow;
        return new LabPowerState(
            lightOn,
            fanOn,
            lightOn ? now : null,
            fanOn ? now : null,
            0,
            0);
    }

    private static LabPowerState ApplyLight(LabPowerState old, bool lightOn, DateTime now)
    {
        var lightSec = old.LightOnSeconds;
        DateTime? since = old.LightOnSince;

        if (old.LightOn && !lightOn && since.HasValue)
        {
            lightSec += Math.Max(0, (now - since.Value).TotalSeconds);
            since = null;
        }
        else if (!old.LightOn && lightOn)
        {
            since = now;
        }

        return old with
        {
            LightOn = lightOn,
            LightOnSince = lightOn ? since ?? now : null,
            LightOnSeconds = lightSec
        };
    }

    private static LabPowerState ApplyFan(LabPowerState old, bool fanOn, DateTime now)
    {
        var fanSec = old.FanOnSeconds;
        DateTime? since = old.FanOnSince;

        if (old.FanOn && !fanOn && since.HasValue)
        {
            fanSec += Math.Max(0, (now - since.Value).TotalSeconds);
            since = null;
        }
        else if (!old.FanOn && fanOn)
        {
            since = now;
        }

        return old with
        {
            FanOn = fanOn,
            FanOnSince = fanOn ? since ?? now : null,
            FanOnSeconds = fanSec
        };
    }

    private static LabPowerState ApplyBoth(LabPowerState old, bool lightOn, bool fanOn, DateTime now)
        => ApplyFan(ApplyLight(old, lightOn, now), fanOn, now);

    private sealed record LabPowerState(
        bool LightOn,
        bool FanOn,
        DateTime? LightOnSince,
        DateTime? FanOnSince,
        double LightOnSeconds,
        double FanOnSeconds);
}

public readonly record struct LabEnergySnapshot(
    bool LightOn,
    bool FanOn,
    double LightRuntimeSeconds,
    double FanRuntimeSeconds,
    double CurrentWatts,
    double EnergyKwh)
{
    public static LabEnergySnapshot Empty => new(false, false, 0, 0, 0, 0);

    public double LightRuntimeHours => LightRuntimeSeconds / 3600.0;
    public double FanRuntimeHours => FanRuntimeSeconds / 3600.0;
    public double LightEnergyKwh => Math.Round(LightRuntimeHours * (LabDeviceStateCache.LightWatts / 1000.0), 4);
    public double FanEnergyKwh => Math.Round(FanRuntimeHours * (LabDeviceStateCache.FanWatts / 1000.0), 4);
}
