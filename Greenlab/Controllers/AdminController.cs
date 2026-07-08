using System.Security.Claims;
using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

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
        ov.ControlMode = HasManualOverride(req) ? ControlMode.Manual : ControlMode.Automatic;
        ov.UpdatedAt = DateTime.UtcNow;

        AddManualCommands(labId, userId, req);
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

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> ListUsers(CancellationToken cancellationToken)
    {
        var rows = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows;
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserListItemDto>> CreateUser([FromBody] CreateUserRequest req, CancellationToken cancellationToken)
    {
        if (!TryParseRole(req.Role, out var role))
            return BadRequest("Invalid role. Use Admin, Teacher, or TeacherAssistant.");

        var email = req.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false))
            return BadRequest("Email is already registered.");

        var user = new User
        {
            Email = email,
            FullName = req.FullName.Trim(),
            Role = role,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(MapUser(user));
    }

    [HttpPut("users/{id:int}")]
    public async Task<ActionResult<UserListItemDto>> UpdateUser(int id, [FromBody] UpdateUserRequest req, CancellationToken cancellationToken)
    {
        if (!TryParseRole(req.Role, out var role))
            return BadRequest("Invalid role. Use Admin, Teacher, or TeacherAssistant.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user == null) return NotFound();

        var email = req.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken).ConfigureAwait(false))
            return BadRequest("Email is already used by another account.");

        if (user.Role == Role.Admin && role != Role.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == Role.Admin && u.IsActive, cancellationToken).ConfigureAwait(false);
            if (adminCount <= 1)
                return BadRequest("Cannot remove the last active admin.");
        }

        user.Email = email;
        user.FullName = req.FullName.Trim();
        user.Role = role;
        user.IsActive = req.IsActive;

        if (!string.IsNullOrWhiteSpace(req.Password))
            user.PasswordHash = _hasher.HashPassword(user, req.Password);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(MapUser(user));
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var currentId = GetUserId();
        if (id == currentId)
            return BadRequest("You cannot delete your own account.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user == null) return NotFound();

        if (user.Role == Role.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == Role.Admin, cancellationToken).ConfigureAwait(false);
            if (adminCount <= 1)
                return BadRequest("Cannot delete the only admin account.");
        }

        if (await _db.LabChangeRequests.AnyAsync(r => r.RequestedByUserId == id, cancellationToken).ConfigureAwait(false))
            return BadRequest("User has lab change requests. Deactivate the account instead or remove those requests first.");

        if (await _db.ManualControlCommands.AnyAsync(c => c.RequestedByUserId == id, cancellationToken).ConfigureAwait(false))
            return BadRequest("User has manual control history. Deactivate the account instead.");

        if (await _db.ScheduleImageImports.AnyAsync(i => i.UploadedByUserId == id, cancellationToken).ConfigureAwait(false))
            return BadRequest("User uploaded schedule imports. Deactivate the account instead.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("sections")]
    public async Task<ActionResult<IReadOnlyList<LabSectionListItemDto>>> ListSections(CancellationToken cancellationToken)
    {
        var rows = await _db.LabSectionSchedules
            .AsNoTracking()
            .Include(s => s.Lab)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Lab.Name)
            .ThenBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new LabSectionListItemDto
            {
                Id = s.Id,
                LabId = s.LabId,
                LabName = s.Lab.Name,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CourseName = s.CourseName
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows;
    }

    [HttpGet("staff-assignments")]
    public async Task<ActionResult<IReadOnlyList<StaffAssignmentListItemDto>>> ListStaffAssignments(CancellationToken cancellationToken)
    {
        var rows = await _db.StaffLabAssignments
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Lab)
            .Where(a => a.IsActive)
            .OrderBy(a => a.User.FullName)
            .ThenBy(a => a.Lab.Name)
            .ThenBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .Select(a => new StaffAssignmentListItemDto
            {
                Id = a.Id,
                UserId = a.UserId,
                StaffName = a.User.FullName,
                StaffEmail = a.User.Email,
                StaffRole = a.User.Role.ToString(),
                LabId = a.LabId,
                LabName = a.Lab.Name,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ValidFrom = a.ValidFrom,
                ValidUntil = a.ValidUntil,
                AssignmentSource = a.AssignmentSource,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows;
    }

    [HttpPost("staff-assignments")]
    public async Task<ActionResult<StaffAssignmentListItemDto>> CreateStaffAssignment(
        [FromBody] CreateStaffAssignmentRequest req,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == req.StaffEmail.Trim(), cancellationToken).ConfigureAwait(false);
        if (user == null)
            return BadRequest($"Unknown staff email: {req.StaffEmail}");
        if (user.Role != Role.Teacher && user.Role != Role.TeacherAssistant)
            return BadRequest($"User {req.StaffEmail} is not Teacher or TeacherAssistant.");

        if (!await _db.Labs.AnyAsync(l => l.Id == req.LabId, cancellationToken).ConfigureAwait(false))
            return BadRequest($"Unknown LabId {req.LabId}");

        var hasWeekly = req.DayOfWeek.HasValue && req.StartTime.HasValue && req.EndTime.HasValue;
        var hasRange = req.ValidFrom.HasValue && req.ValidUntil.HasValue;
        if (!hasWeekly && !hasRange)
            return BadRequest("Provide either (dayOfWeek + startTime + endTime) or (validFrom + validUntil).");

        var row = new StaffLabAssignment
        {
            UserId = user.Id,
            LabId = req.LabId,
            DayOfWeek = req.DayOfWeek,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            ValidFrom = req.ValidFrom,
            ValidUntil = req.ValidUntil,
            AssignmentSource = StaffAssignmentSource.ManualAdmin,
            IsActive = true
        };
        _db.StaffLabAssignments.Add(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var lab = await _db.Labs.AsNoTracking().FirstAsync(l => l.Id == req.LabId, cancellationToken).ConfigureAwait(false);
        return Ok(new StaffAssignmentListItemDto
        {
            Id = row.Id,
            UserId = user.Id,
            StaffName = user.FullName,
            StaffEmail = user.Email,
            StaffRole = user.Role.ToString(),
            LabId = lab.Id,
            LabName = lab.Name,
            DayOfWeek = row.DayOfWeek,
            StartTime = row.StartTime,
            EndTime = row.EndTime,
            ValidFrom = row.ValidFrom,
            ValidUntil = row.ValidUntil,
            AssignmentSource = row.AssignmentSource,
            IsActive = row.IsActive
        });
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
                EndTime = null,
                AssignmentSource = StaffAssignmentSource.AdminApprovedTransfer
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

    private static UserListItemDto MapUser(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Role = u.Role.ToString(),
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };

    private static bool TryParseRole(string role, out Role parsed) =>
        Enum.TryParse(role, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
