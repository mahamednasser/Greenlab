using System.Security.Claims;
using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Models;
using Greenlab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/staff")]
public class StaffController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILabAccessService _access;

    public StaffController(AppDbContext db, ILabAccessService access)
    {
        _db = db;
        _access = access;
    }

    public record CurrentLabDto(int Id, string Name, bool InSession, LabOverrideDto? Override);

    [HttpGet("me/current-labs")]
    public async Task<ActionResult<IReadOnlyList<CurrentLabDto>>> CurrentLabs(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        var tod = now.TimeOfDay;
        var dow = now.DayOfWeek;

        var ids = await _access.GetStaffAllowedLabIdsAsync(userId, now, cancellationToken).ConfigureAwait(false);
        if (ids.Count == 0)
            return new List<CurrentLabDto>();

        var labs = await _db.Labs
            .AsNoTracking()
            .Include(l => l.ControlOverride)
            .Include(l => l.SectionSchedules)
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return labs.Select(l => new CurrentLabDto(
            l.Id,
            l.Name,
            l.SectionSchedules.Any(s => s.DayOfWeek == dow && s.StartTime <= tod && s.EndTime >= tod),
            l.ControlOverride == null
                ? null
                : new LabOverrideDto
                {
                    MotionSensorDisabled = l.ControlOverride.MotionSensorDisabled,
                    ForceLightOn = l.ControlOverride.ForceLightOn,
                    ForceFanOn = l.ControlOverride.ForceFanOn,
                    ForceRelay3On = l.ControlOverride.ForceRelay3On,
                    ForceRelay4On = l.ControlOverride.ForceRelay4On
                })).ToList();
    }

    [HttpPost("labs/{labId:int}/override")]
    public async Task<ActionResult> SetOverride(int labId, [FromBody] SetLabOverrideRequest req, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        if (!await _access.CanStaffAccessLabAsync(userId, labId, now, cancellationToken).ConfigureAwait(false))
            return Forbid();

        var lab = await _db.Labs.FirstOrDefaultAsync(l => l.Id == labId, cancellationToken).ConfigureAwait(false);
        if (lab == null) return NotFound();

        var ov = await _db.LabControlOverrides.FirstOrDefaultAsync(o => o.LabId == labId, cancellationToken).ConfigureAwait(false);
        if (ov == null)
        {
            ov = new LabControlOverride { LabId = labId };
            _db.LabControlOverrides.Add(ov);
        }

        if (req.MotionSensorDisabled.HasValue) ov.MotionSensorDisabled = req.MotionSensorDisabled.Value;
        if (req.ForceLightOn.HasValue) ov.ForceLightOn = req.ForceLightOn;
        if (req.ForceFanOn.HasValue) ov.ForceFanOn = req.ForceFanOn;
        if (req.ForceRelay3On.HasValue) ov.ForceRelay3On = req.ForceRelay3On;
        if (req.ForceRelay4On.HasValue) ov.ForceRelay4On = req.ForceRelay4On;

        ov.SetByUserId = userId;
        ov.ControlMode = HasManualOverride(req) ? ControlMode.Manual : ControlMode.Automatic;
        ov.UpdatedAt = DateTime.UtcNow;

        AddManualCommands(labId, userId, req);
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "StaffOverride",
            LabId = labId,
            UserId = userId,
            Details = System.Text.Json.JsonSerializer.Serialize(req)
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("lab-change-requests")]
    public async Task<ActionResult<int>> CreateLabChangeRequest([FromBody] CreateLabChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var now = DateTime.UtcNow;
        if (!await _access.CanStaffAccessLabAsync(userId, dto.FromLabId, now, cancellationToken).ConfigureAwait(false))
            return Forbid();

        if (dto.SectionEnd <= dto.SectionStart)
            return BadRequest("SectionEnd must be after SectionStart.");

        if (!await _db.Labs.AnyAsync(l => l.Id == dto.ToLabId, cancellationToken).ConfigureAwait(false))
            return BadRequest("Unknown ToLabId.");

        var autoApprove = await IsLabFreeAsync(dto.ToLabId, dto.SectionStart, dto.SectionEnd, cancellationToken).ConfigureAwait(false);
        var r = new LabChangeRequest
        {
            RequestedByUserId = userId,
            FromLabId = dto.FromLabId,
            ToLabId = dto.ToLabId,
            SectionStart = dto.SectionStart,
            SectionEnd = dto.SectionEnd,
            Status = autoApprove ? LabChangeRequestStatus.AutoApproved : LabChangeRequestStatus.Pending,
            ReviewedAt = autoApprove ? DateTime.UtcNow : null,
            ReviewNote = autoApprove ? "Auto-approved because target lab is free." : null
        };
        _db.LabChangeRequests.Add(r);

        if (autoApprove)
        {
            _db.StaffLabAssignments.Add(new StaffLabAssignment
            {
                UserId = userId,
                LabId = dto.ToLabId,
                ValidFrom = dto.SectionStart,
                ValidUntil = dto.SectionEnd,
                AssignmentSource = StaffAssignmentSource.AutoApprovedTransfer
            });
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "LabChangeAutoApproved",
                UserId = userId,
                LabId = dto.ToLabId,
                Details = $"From lab {dto.FromLabId}; {dto.SectionStart:o} - {dto.SectionEnd:o}"
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return r.Id;
    }

    [HttpGet("lab-change-requests/mine")]
    public async Task<ActionResult<IReadOnlyList<LabChangeRequestDto>>> MyLabChangeRequests(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var rows = await _db.LabChangeRequests
            .AsNoTracking()
            .Include(r => r.RequestedBy)
            .Include(r => r.FromLab)
            .Include(r => r.ToLab)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows.Select(r => new LabChangeRequestDto
        {
            Id = r.Id,
            RequestedByUserId = r.RequestedByUserId,
            RequestedByName = r.RequestedBy.FullName,
            FromLabId = r.FromLabId,
            FromLabName = r.FromLab.Name,
            ToLabId = r.ToLabId,
            ToLabName = r.ToLab.Name,
            SectionStart = r.SectionStart,
            SectionEnd = r.SectionEnd,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            ReviewNote = r.ReviewNote
        }).ToList();
    }

    private async Task<bool> IsLabFreeAsync(int labId, DateTime sectionStart, DateTime sectionEnd, CancellationToken cancellationToken)
    {
        var day = sectionStart.DayOfWeek;
        var start = sectionStart.TimeOfDay;
        var end = sectionEnd.TimeOfDay;
        return !await _db.LabSectionSchedules.AsNoTracking().AnyAsync(s =>
            s.LabId == labId
            && s.IsActive
            && s.DayOfWeek == day
            && s.StartTime < end
            && s.EndTime > start,
            cancellationToken).ConfigureAwait(false);
    }

    private void AddManualCommands(int labId, int userId, SetLabOverrideRequest req)
    {
        if (req.MotionSensorDisabled.HasValue)
            AddManualCommand(labId, userId, ManualControlTarget.MotionSensor, req.MotionSensorDisabled.Value.ToString());
        if (req.ForceLightOn.HasValue)
            AddManualCommand(labId, userId, ManualControlTarget.Light, req.ForceLightOn.Value.ToString());
        if (req.ForceFanOn.HasValue)
            AddManualCommand(labId, userId, ManualControlTarget.Fan, req.ForceFanOn.Value.ToString());
        if (req.ForceRelay3On.HasValue)
            AddManualCommand(labId, userId, ManualControlTarget.Relay3, req.ForceRelay3On.Value.ToString());
        if (req.ForceRelay4On.HasValue)
            AddManualCommand(labId, userId, ManualControlTarget.Relay4, req.ForceRelay4On.Value.ToString());
    }

    private void AddManualCommand(int labId, int userId, ManualControlTarget target, string value)
    {
        _db.ManualControlCommands.Add(new ManualControlCommand
        {
            LabId = labId,
            RequestedByUserId = userId,
            Target = target,
            RequestedValue = value,
            CommandStatus = ManualControlCommandStatus.Sent
        });
    }

    private static bool HasManualOverride(SetLabOverrideRequest req) =>
        req.MotionSensorDisabled == true
        || req.ForceLightOn.HasValue
        || req.ForceFanOn.HasValue
        || req.ForceRelay3On.HasValue
        || req.ForceRelay4On.HasValue;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
