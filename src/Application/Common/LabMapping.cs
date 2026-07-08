// Layer: Application
// Responsibility: Maps lab domain entities into application DTOs.
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;

namespace Greenlab.Application.Common;

internal static class LabMapping
{
    public static LabDetailDto ToDetailDto(Lab lab) => new()
    {
        Id = lab.Id,
        Name = lab.Name,
        DeviceKey = lab.DeviceKey,
        LightThreshold = lab.LightThreshold,
        TempFanOnThresholdCelsius = lab.TempFanOnThresholdCelsius,
        CvLabCode = lab.CvLabCode,
        CameraRtspUrl = lab.CameraRtspUrl
    };

    public static LabDeviceDto ToDeviceDto(LabDevice device) => new()
    {
        Id = device.Id,
        LabId = device.LabId,
        Name = device.Name,
        DeviceType = device.DeviceType,
        IpAddress = device.IpAddress,
        IsActive = device.IsActive,
        LastSeenAt = device.LastSeenAt,
        CreatedAt = device.CreatedAt
    };

    public static LabOverrideDto? ToOverrideDto(LabControlOverride? controlOverride)
    {
        if (controlOverride == null)
            return null;

        return new LabOverrideDto
        {
            MotionSensorDisabled = controlOverride.MotionSensorDisabled,
            ForceLightOn = controlOverride.ForceLightOn,
            ForceFanOn = controlOverride.ForceFanOn,
            ForceRelay3On = controlOverride.ForceRelay3On,
            ForceRelay4On = controlOverride.ForceRelay4On
        };
    }

    public static LabChangeRequestDto ToLabChangeRequestDto(LabChangeRequest request) => new()
    {
        Id = request.Id,
        RequestedByUserId = request.RequestedByUserId,
        RequestedByName = request.RequestedBy.FullName,
        FromLabId = request.FromLabId,
        FromLabName = request.FromLab.Name,
        ToLabId = request.ToLabId,
        ToLabName = request.ToLab.Name,
        SectionStart = request.SectionStart,
        SectionEnd = request.SectionEnd,
        Status = request.Status,
        CreatedAt = request.CreatedAt,
        ReviewNote = request.ReviewNote
    };
}
