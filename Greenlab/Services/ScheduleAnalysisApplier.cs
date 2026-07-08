using System.Security.Cryptography;
using System.Text;
using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Models;
using Greenlab.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Greenlab.Services;

public class ScheduleAnalysisApplier : IScheduleAnalysisApplier
{
    private readonly AppDbContext _db;
    private readonly ImportedUserOptions _imported;
    private readonly PasswordHasher<User> _hasher = new();

    public ScheduleAnalysisApplier(AppDbContext db, IOptions<ImportedUserOptions> imported)
    {
        _db = db;
        _imported = imported.Value;
    }

    public async Task<ApplyScheduleAnalysisResultDto> ApplyAsync(ApplyScheduleAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ApplyScheduleAnalysisResultDto();
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var labByName = new Dictionary<string, Lab>(StringComparer.OrdinalIgnoreCase);
        var existingLabs = await _db.Labs.ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var l in existingLabs)
            labByName[l.Name] = l;

        var brandNewLabs = new List<Lab>();
        foreach (var p in request.Labs)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) continue;
            var name = p.Name.Trim();
            if (labByName.TryGetValue(name, out var existing))
            {
                result.LabsMatchedExisting++;
                if (!string.IsNullOrWhiteSpace(p.CvLabCode)) existing.CvLabCode = p.CvLabCode.Trim();
                continue;
            }

            var key = string.IsNullOrWhiteSpace(p.DeviceKey)
                ? Guid.NewGuid().ToString("N")
                : p.DeviceKey.Trim();
            var lab = new Lab
            {
                Name = name,
                DeviceKey = key,
                LightThreshold = 2000,
                TempFanOnThresholdCelsius = 28f,
                CvLabCode = string.IsNullOrWhiteSpace(p.CvLabCode) ? null : p.CvLabCode.Trim()
            };
            _db.Labs.Add(lab);
            brandNewLabs.Add(lab);
            labByName[name] = lab;
            result.LabsCreated++;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var lab in brandNewLabs)
        {
            _db.LabControlOverrides.Add(new LabControlOverride { LabId = lab.Id, UpdatedAt = DateTime.UtcNow });
            _db.LabDevices.Add(new LabDevice
            {
                LabId = lab.Id,
                Name = $"{lab.Name} ESP32",
                DeviceKeyHash = DeviceKeyHasher.Hash(lab.DeviceKey),
                DeviceType = LabDeviceType.Esp32Controller
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var s in request.Sections)
        {
            if (!labByName.TryGetValue(s.LabName.Trim(), out var lab))
            {
                result.Warnings.Add($"Section skipped: unknown lab '{s.LabName}'.");
                continue;
            }

            _db.LabSectionSchedules.Add(new LabSectionSchedule
            {
                LabId = lab.Id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CourseName = s.CourseName
            });
            result.SectionsAdded++;
        }

        foreach (var a in request.StaffAssignments)
        {
            if (!labByName.TryGetValue(a.LabName.Trim(), out var lab))
            {
                result.Warnings.Add($"Staff row skipped: unknown lab '{a.LabName}'.");
                continue;
            }

            if (!TryParseRole(a.Role, out var role))
            {
                result.Warnings.Add($"Staff row skipped for '{a.PersonName}': invalid role '{a.Role}'.");
                continue;
            }

            var user = await FindOrCreateUserAsync(a.PersonName, role, request.CreateMissingUsers, result, cancellationToken).ConfigureAwait(false);
            if (user == null)
            {
                result.Warnings.Add($"Staff row skipped for '{a.PersonName}': user not found and CreateMissingUsers is false.");
                continue;
            }

            _db.StaffLabAssignments.Add(new StaffLabAssignment
            {
                UserId = user.Id,
                LabId = lab.Id,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ValidFrom = null,
                ValidUntil = null
            });
            result.StaffAssignmentsAdded++;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<User?> FindOrCreateUserAsync(
        string personName,
        Role role,
        bool createMissing,
        ApplyScheduleAnalysisResultDto result,
        CancellationToken cancellationToken)
    {
        var trimmed = personName.Trim();
        var email = BuildImportedEmail(trimmed);

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email || u.FullName == trimmed, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            if (existing.Role != role && existing.Role != Role.Admin)
                result.Warnings.Add($"User '{trimmed}' exists with role {existing.Role}; assignment still added.");
            return existing;
        }

        if (!createMissing)
            return null;

        var user = new User
        {
            Email = email,
            FullName = trimmed,
            Role = role
        };
        user.PasswordHash = _hasher.HashPassword(user, _imported.DefaultPassword);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        result.UsersCreated++;
        return user;
    }

    private static string BuildImportedEmail(string fullName)
    {
        var letters = new string(fullName.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (letters.Length < 2) letters = "user";
        if (letters.Length > 48) letters = letters[..48];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullName.Trim())))[..10].ToLowerInvariant();
        return $"{letters}.{hash}@import.greenlab.local";
    }

    private static bool TryParseRole(string role, out Role r)
    {
        r = Role.TeacherAssistant;
        var x = role.Trim();
        if (string.Equals(x, nameof(Role.Teacher), StringComparison.OrdinalIgnoreCase))
        {
            r = Role.Teacher;
            return true;
        }

        if (string.Equals(x, nameof(Role.TeacherAssistant), StringComparison.OrdinalIgnoreCase)
            || string.Equals(x, "TA", StringComparison.OrdinalIgnoreCase)
            || x.Contains("assistant", StringComparison.OrdinalIgnoreCase))
        {
            r = Role.TeacherAssistant;
            return true;
        }

        return false;
    }
}
