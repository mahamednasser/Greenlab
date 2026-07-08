
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(rows);
    }

    [HttpPost("staff-assignments")]
    public async Task<ActionResult<StaffAssignmentListItemDto>> CreateStaffAssignment(
        [FromBody] CreateStaffAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.StaffEmail.Trim();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);
        if (user == null)
            return BadRequest($"Unknown staff email: {request.StaffEmail}");

        if (user.Role != Role.Teacher && user.Role != Role.TeacherAssistant)
            return BadRequest($"User {request.StaffEmail} is not Teacher or TeacherAssistant.");

        var lab = await _db.Labs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LabId, cancellationToken)
            .ConfigureAwait(false);
        if (lab == null)
            return BadRequest($"Unknown LabId {request.LabId}");

        var hasWeekly = request.DayOfWeek.HasValue && request.StartTime.HasValue && request.EndTime.HasValue;
        var hasRange = request.ValidFrom.HasValue && request.ValidUntil.HasValue;
        if (!hasWeekly && !hasRange)
            return BadRequest("Provide either (dayOfWeek + startTime + endTime) or (validFrom + validUntil).");

        if (hasWeekly && request.StartTime >= request.EndTime)
            return BadRequest("For weekly schedule, startTime must be before endTime.");

        if (hasRange && request.ValidFrom >= request.ValidUntil)
            return BadRequest("For date range, validFrom must be before validUntil.");

        var row = new StaffLabAssignment
        {
            UserId = user.Id,
            LabId = request.LabId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            AssignmentSource = StaffAssignmentSource.ManualAdmin,
            IsActive = true
        };

        _db.StaffLabAssignments.Add(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
    public async Task<IActionResult> ReviewLabChange(int id, [FromBody] ReviewLabChangeRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ReviewLabChangeRequestCommand(id, GetUserId(), request), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
