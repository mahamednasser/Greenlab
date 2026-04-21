using System.Security.Claims;
using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("labs/overview")]
    public async Task<ActionResult<IReadOnlyList<LabOverviewDto>>> LabsOverview(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tod = now.TimeOfDay;
        var dow = now.DayOfWeek;

        var labs = await _db.Labs
            .AsNoTracking()
            .Include(l => l.ControlOverride)
            .Include(l => l.SectionSchedules)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var list = labs.Select(l => new LabOverviewDto
        {
            Id = l.Id,
            Name = l.Name,
            LastOccupancyCount = l.LastOccupancyCount,
            LastOccupancyAt = l.LastOccupancyAt,
            InSession = l.SectionSchedules.Any(s =>
                s.DayOfWeek == dow && s.StartTime <= tod && s.EndTime >= tod),
            Override = l.ControlOverride == null
                ? null
                : new LabOverrideDto
                {
                    MotionSensorDisabled = l.ControlOverride.MotionSensorDisabled,
                    ForceLightOn = l.ControlOverride.ForceLightOn,
                    ForceFanOn = l.ControlOverride.ForceFanOn,
                    ForceRelay3On = l.ControlOverride.ForceRelay3On,
                    ForceRelay4On = l.ControlOverride.ForceRelay4On
                }
        }).ToList();

        return list;
    }

    [HttpPost("labs/{labId:int}/override")]
    public async Task<ActionResult> SetOverride(int labId, [FromBody] SetLabOverrideRequest req, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.FirstOrDefaultAsync(l => l.Id == labId, cancellationToken).ConfigureAwait(false);
        if (lab == null) return NotFound();

        var userId = GetUserId();
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
        ov.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "AdminOverride",
            LabId = labId,
            UserId = userId,
            Details = System.Text.Json.JsonSerializer.Serialize(req)
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("lab-change-requests")]
    public async Task<ActionResult<IReadOnlyList<LabChangeRequestDto>>> LabChangeRequests(
        [FromQuery] LabChangeRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var q = _db.LabChangeRequests
            .AsNoTracking()
            .Include(r => r.RequestedBy)
            .Include(r => r.FromLab)
            .Include(r => r.ToLab)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);

        var rows = await q.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(MapRequest).ToList();
    }

    [HttpPost("lab-change-requests/{id:int}/review")]
    public async Task<ActionResult> ReviewLabChange(int id, [FromBody] ReviewLabChangeRequest req, CancellationToken cancellationToken)
    {
        var r = await _db.LabChangeRequests
            .Include(x => x.RequestedBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (r == null) return NotFound();
        if (r.Status != LabChangeRequestStatus.Pending)
            return BadRequest("Already reviewed.");

        var adminId = GetUserId();
        r.ReviewedByUserId = adminId;
        r.ReviewedAt = DateTime.UtcNow;
        r.ReviewNote = req.Note;

        if (req.Approve)
        {
            r.Status = LabChangeRequestStatus.Approved;
            _db.StaffLabAssignments.Add(new StaffLabAssignment
            {
                UserId = r.RequestedByUserId,
                LabId = r.ToLabId,
                ValidFrom = r.SectionStart,
                ValidUntil = r.SectionEnd,
                DayOfWeek = null,
                StartTime = null,
                EndTime = null
            });
        }
        else
        {
            r.Status = LabChangeRequestStatus.Rejected;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            Action = req.Approve ? "LabChangeApproved" : "LabChangeRejected",
            UserId = adminId,
            Details = $"Request {id}"
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private static LabChangeRequestDto MapRequest(LabChangeRequest r) => new()
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
    };

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
