
using System.Security.Claims;
using Greenlab.Application.DTOs;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.Features.LabChangeRequests.Commands;
using Greenlab.Application.Features.LabChangeRequests.Queries;
using Greenlab.Application.Features.Labs.Commands;
using Greenlab.Application.Features.Labs.Queries;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ISender _sender;
    private readonly AppDbContext _db;
    private readonly IPasswordHashService _passwords;

    public AdminController(ISender sender, AppDbContext db, IPasswordHashService passwords)
    {
        _sender = sender;
        _db = db;
        _passwords = passwords;
    }

    [HttpGet("labs/overview")]
    public async Task<ActionResult<IReadOnlyList<LabOverviewDto>>> LabsOverview(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetLabsOverviewQuery(), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("labs/{labId:int}/override")]
    public async Task<IActionResult> SetOverride(int labId, [FromBody] SetLabOverrideRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetLabOverrideCommand(labId, GetUserId(), request, RequireStaffAccess: false, AuditAction: "AdminOverride"), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("lab-change-requests")]
    public async Task<ActionResult<IReadOnlyList<LabChangeRequestDto>>> LabChangeRequests([FromQuery] LabChangeRequestStatus? status, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new ListLabChangeRequestsQuery(status), cancellationToken).ConfigureAwait(false));
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(rows);
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserListItemDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Role>(request.Role, true, out var role))
            return BadRequest("Invalid role. Use Admin, Teacher, or TeacherAssistant.");

        var email = request.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken).ConfigureAwait(false))
            return BadRequest("Email is already registered.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = role,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwords.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new UserListItemDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPut("users/{id:int}")]
    public async Task<ActionResult<UserListItemDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Role>(request.Role, true, out var role))
            return BadRequest("Invalid role. Use Admin, Teacher, or TeacherAssistant.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return NotFound();

        var email = request.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken).ConfigureAwait(false))
            return BadRequest("Email is already used by another account.");

        user.Email = email;
        user.FullName = request.FullName.Trim();
        user.Role = role;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _passwords.HashPassword(user, request.Password);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new UserListItemDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
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
                DayOfWeek = (int)s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CourseName = s.CourseName
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(rows);
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
                DayOfWeek = a.DayOfWeek.HasValue ? (int)a.DayOfWeek.Value : null,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ValidFrom = a.ValidFrom,
                ValidUntil = a.ValidUntil,
                AssignmentSource = a.AssignmentSource,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(rows);
    }

    [HttpPost("staff-assignments")]
    public async Task<ActionResult<StaffAssignmentListItemDto>> CreateStaffAssignment(
        [FromBody] CreateStaffAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var (error, built) = await TryBuildAssignmentAsync(request, cancellationToken).ConfigureAwait(false);
        if (error != null || built == null)
            return BadRequest(error);

        var row = new StaffLabAssignment
        {
            UserId = built.User.Id,
            LabId = built.Lab.Id,
            DayOfWeek = built.Day,
            StartTime = built.Start,
            EndTime = built.End,
            ValidFrom = built.From,
            ValidUntil = built.Until,
            AssignmentSource = StaffAssignmentSource.ManualAdmin,
            IsActive = true
        };

        _db.StaffLabAssignments.Add(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ToStaffAssignmentDto(row, built.User, built.Lab));
    }

    [HttpPut("staff-assignments/{id:int}")]
    public async Task<ActionResult<StaffAssignmentListItemDto>> UpdateStaffAssignment(
        int id,
        [FromBody] CreateStaffAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var row = await _db.StaffLabAssignments
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return NotFound();

        var (error, built) = await TryBuildAssignmentAsync(request, cancellationToken).ConfigureAwait(false);
        if (error != null || built == null)
            return BadRequest(error);

        row.UserId = built.User.Id;
        row.LabId = built.Lab.Id;
        row.DayOfWeek = built.Day;
        row.StartTime = built.Start;
        row.EndTime = built.End;
        row.ValidFrom = built.From;
        row.ValidUntil = built.Until;
        row.AssignmentSource = StaffAssignmentSource.ManualAdmin;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ToStaffAssignmentDto(row, built.User, built.Lab));
    }

    [HttpDelete("staff-assignments/{id:int}")]
    public async Task<IActionResult> DeleteStaffAssignment(int id, CancellationToken cancellationToken)
    {
        var row = await _db.StaffLabAssignments
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return NotFound();

        row.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("lab-change-requests/{id:int}/review")]
    public async Task<IActionResult> ReviewLabChange(int id, [FromBody] ReviewLabChangeRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ReviewLabChangeRequestCommand(id, GetUserId(), request), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs(CancellationToken cancellationToken)
    {
        var logs = await _db.AuditLogs
            .AsNoTracking()
            .Include(l => l.Lab)
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                Action = l.Action,
                Details = l.Details,
                LabId = l.LabId,
                LabName = l.Lab != null ? l.Lab.Name : null,
                UserId = l.UserId,
                UserFullName = l.User != null ? l.User.FullName : null,
                UserEmail = l.User != null ? l.User.Email : null,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(logs);
    }

    private sealed record BuiltStaffAssignment(
        User User,
        Lab Lab,
        DayOfWeek? Day,
        TimeSpan? Start,
        TimeSpan? End,
        DateTime? From,
        DateTime? Until);

    private async Task<(string? Error, BuiltStaffAssignment? Built)>
        TryBuildAssignmentAsync(CreateStaffAssignmentRequest request, CancellationToken cancellationToken)
    {
        var email = request.StaffEmail.Trim();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);
        if (user == null)
            return ($"Unknown staff email: {request.StaffEmail}", null);

        if (user.Role != Role.Teacher && user.Role != Role.TeacherAssistant)
            return ($"User {request.StaffEmail} is not Teacher or TeacherAssistant.", null);

        var lab = await _db.Labs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LabId, cancellationToken)
            .ConfigureAwait(false);
        if (lab == null)
            return ($"Unknown LabId {request.LabId}", null);

        DayOfWeek? day = null;
        if (request.DayOfWeek.HasValue)
        {
            if (request.DayOfWeek.Value < 0 || request.DayOfWeek.Value > 6)
                return ("dayOfWeek must be 0 (Sunday) through 6 (Saturday).", null);
            day = (DayOfWeek)request.DayOfWeek.Value;
        }

        var hasWeekly = day.HasValue && request.StartTime.HasValue && request.EndTime.HasValue;
        var hasRange = request.ValidFrom.HasValue && request.ValidUntil.HasValue;
        if (!hasWeekly && !hasRange)
            return ("Provide either (dayOfWeek + startTime + endTime) or (validFrom + validUntil).", null);

        if (hasWeekly && request.StartTime >= request.EndTime)
            return ("For weekly schedule, startTime must be before endTime.", null);

        if (hasRange && request.ValidFrom >= request.ValidUntil)
            return ("For date range, validFrom must be before validUntil.", null);

        return (null, new BuiltStaffAssignment(
            user,
            lab,
            hasWeekly ? day : null,
            hasWeekly ? request.StartTime : null,
            hasWeekly ? request.EndTime : null,
            hasRange ? request.ValidFrom : null,
            hasRange ? request.ValidUntil : null));
    }

    private static StaffAssignmentListItemDto ToStaffAssignmentDto(StaffLabAssignment row, User user, Lab lab) => new()
    {
        Id = row.Id,
        UserId = user.Id,
        StaffName = user.FullName,
        StaffEmail = user.Email,
        StaffRole = user.Role.ToString(),
        LabId = lab.Id,
        LabName = lab.Name,
        DayOfWeek = row.DayOfWeek.HasValue ? (int)row.DayOfWeek.Value : null,
        StartTime = row.StartTime,
        EndTime = row.EndTime,
        ValidFrom = row.ValidFrom,
        ValidUntil = row.ValidUntil,
        AssignmentSource = row.AssignmentSource,
        IsActive = row.IsActive
    };

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
