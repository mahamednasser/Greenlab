// Layer: Application
// Responsibility: Handles schedule import, analysis, and apply use cases.
using System.Security.Cryptography;
using System.Text;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Exceptions;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Schedules.Commands;

public record ImportScheduleImageCommand(string FileName, string ContentType, long FileSizeBytes, byte[] Content, int UploadedByUserId, string? Notes) : IRequest<ScheduleImageImportResult>;

public record ScheduleImageImportResult(int Id, string StoredPath, string FileName);

public record ImportScheduleDataCommand(BulkScheduleImportRequest Request) : IRequest;

public record AnalyzeScheduleImageCommand(string FileName, string ContentType, long FileSizeBytes, byte[] Content, bool Persist, int UploadedByUserId, string? Notes) : IRequest<AnalyzeScheduleImageResult>;

public record AnalyzeScheduleImageResult(int? ImportId, ScheduleAnalysisResultDto Analysis);

public record AnalyzeScheduleImportCommand(int ImportId) : IRequest<ScheduleAnalysisResultDto>;

public record ApplyScheduleAnalysisCommand(ApplyScheduleAnalysisRequest Request) : IRequest<ApplyScheduleAnalysisResultDto>;

public class ImportScheduleImageCommandHandler : IRequestHandler<ImportScheduleImageCommand, ScheduleImageImportResult>
{
    private readonly IFileStorageService _storage;
    private readonly IScheduleImageImportRepository _imports;
    private readonly IUnitOfWork _unitOfWork;

    public ImportScheduleImageCommandHandler(IFileStorageService storage, IScheduleImageImportRepository imports, IUnitOfWork unitOfWork)
    {
        _storage = storage;
        _imports = imports;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleImageImportResult> Handle(ImportScheduleImageCommand request, CancellationToken cancellationToken)
    {
        var storedPath = await _storage.SaveScheduleImageAsync(request.FileName, request.Content, cancellationToken).ConfigureAwait(false);
        var row = new ScheduleImageImport
        {
            FileName = request.FileName,
            StoredPath = storedPath,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            UploadedByUserId = request.UploadedByUserId,
            Notes = request.Notes
        };

        await _imports.AddAsync(row, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ScheduleImageImportResult(row.Id, row.StoredPath, row.FileName);
    }
}

public class ImportScheduleDataCommandHandler : IRequestHandler<ImportScheduleDataCommand>
{
    private readonly ILabRepository _labs;
    private readonly IUserRepository _users;
    private readonly ILabSectionScheduleRepository _sections;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;

    public ImportScheduleDataCommandHandler(
        ILabRepository labs,
        IUserRepository users,
        ILabSectionScheduleRepository sections,
        IStaffLabAssignmentRepository assignments,
        IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _users = users;
        _sections = sections;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ImportScheduleDataCommand request, CancellationToken cancellationToken)
    {
        foreach (var section in request.Request.LabSections)
        {
            if (!await _labs.ExistsAsync(section.LabId, cancellationToken).ConfigureAwait(false))
                throw new DomainException($"Unknown LabId {section.LabId}");

            await _sections.AddAsync(new LabSectionSchedule
            {
                LabId = section.LabId,
                DayOfWeek = section.DayOfWeek,
                StartTime = section.StartTime,
                EndTime = section.EndTime,
                CourseName = section.CourseName
            }, cancellationToken).ConfigureAwait(false);
        }

        foreach (var assignment in request.Request.StaffAssignments)
        {
            var user = await _users.GetByEmailAsync(assignment.StaffEmail, track: true, cancellationToken).ConfigureAwait(false)
                ?? throw new DomainException($"Unknown staff email: {assignment.StaffEmail}");

            if (!_users.IsStaffRole(user.Role))
                throw new DomainException($"User {assignment.StaffEmail} is not Teacher or TeacherAssistant.");

            if (!await _labs.ExistsAsync(assignment.LabId, cancellationToken).ConfigureAwait(false))
                throw new DomainException($"Unknown LabId {assignment.LabId}");

            var hasWeekly = assignment.DayOfWeek.HasValue && assignment.StartTime.HasValue && assignment.EndTime.HasValue;
            var hasRange = assignment.ValidFrom.HasValue && assignment.ValidUntil.HasValue;
            if (!hasWeekly && !hasRange)
                throw new DomainException("Each staff assignment needs either (DayOfWeek+Start+End) or (ValidFrom+ValidUntil).");

            await _assignments.AddAsync(new StaffLabAssignment
            {
                UserId = user.Id,
                LabId = assignment.LabId,
                DayOfWeek = assignment.DayOfWeek,
                StartTime = assignment.StartTime,
                EndTime = assignment.EndTime,
                ValidFrom = assignment.ValidFrom,
                ValidUntil = assignment.ValidUntil
            }, cancellationToken).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public class AnalyzeScheduleImageCommandHandler : IRequestHandler<AnalyzeScheduleImageCommand, AnalyzeScheduleImageResult>
{
    private readonly IFileStorageService _storage;
    private readonly IScheduleImageImportRepository _imports;
    private readonly IScheduleImageAnalysisService _vision;
    private readonly IUnitOfWork _unitOfWork;

    public AnalyzeScheduleImageCommandHandler(IFileStorageService storage, IScheduleImageImportRepository imports, IScheduleImageAnalysisService vision, IUnitOfWork unitOfWork)
    {
        _storage = storage;
        _imports = imports;
        _vision = vision;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnalyzeScheduleImageResult> Handle(AnalyzeScheduleImageCommand request, CancellationToken cancellationToken)
    {
        int? importId = null;
        if (request.Persist)
        {
            var storedPath = await _storage.SaveScheduleImageAsync(request.FileName, request.Content, cancellationToken).ConfigureAwait(false);
            var row = new ScheduleImageImport
            {
                FileName = request.FileName,
                StoredPath = storedPath,
                ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "image/jpeg" : request.ContentType,
                FileSizeBytes = request.FileSizeBytes,
                UploadedByUserId = request.UploadedByUserId,
                Notes = request.Notes
            };
            await _imports.AddAsync(row, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            importId = row.Id;
        }

        var analysis = await _vision.AnalyzeImageAsync(request.Content, string.IsNullOrWhiteSpace(request.ContentType) ? "image/jpeg" : request.ContentType, cancellationToken).ConfigureAwait(false);
        return new AnalyzeScheduleImageResult(importId, analysis);
    }
}

public class AnalyzeScheduleImportCommandHandler : IRequestHandler<AnalyzeScheduleImportCommand, ScheduleAnalysisResultDto>
{
    private readonly IScheduleImageImportRepository _imports;
    private readonly IFileStorageService _storage;
    private readonly IScheduleImageAnalysisService _vision;

    public AnalyzeScheduleImportCommandHandler(IScheduleImageImportRepository imports, IFileStorageService storage, IScheduleImageAnalysisService vision)
    {
        _imports = imports;
        _storage = storage;
        _vision = vision;
    }

    public async Task<ScheduleAnalysisResultDto> Handle(AnalyzeScheduleImportCommand request, CancellationToken cancellationToken)
    {
        var row = await _imports.GetByIdAsync(request.ImportId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("ScheduleImageImport", request.ImportId);

        if (!_storage.Exists(row.StoredPath))
            throw new DomainException("Stored file is missing on disk.");

        var bytes = await _storage.ReadFileAsync(row.StoredPath, cancellationToken).ConfigureAwait(false);
        return await _vision.AnalyzeImageAsync(bytes, row.ContentType, cancellationToken).ConfigureAwait(false);
    }
}

public class ApplyScheduleAnalysisCommandHandler : IRequestHandler<ApplyScheduleAnalysisCommand, ApplyScheduleAnalysisResultDto>
{
    private readonly ILabRepository _labs;
    private readonly ILabControlOverrideRepository _overrides;
    private readonly ILabDeviceRepository _devices;
    private readonly ILabSectionScheduleRepository _sections;
    private readonly IStaffLabAssignmentRepository _assignments;
    private readonly IUserRepository _users;
    private readonly IDeviceKeyHasher _keyHasher;
    private readonly IPasswordHashService _passwords;
    private readonly IImportedUserDefaults _importedUserDefaults;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyScheduleAnalysisCommandHandler(
        ILabRepository labs,
        ILabControlOverrideRepository overrides,
        ILabDeviceRepository devices,
        ILabSectionScheduleRepository sections,
        IStaffLabAssignmentRepository assignments,
        IUserRepository users,
        IDeviceKeyHasher keyHasher,
        IPasswordHashService passwords,
        IImportedUserDefaults importedUserDefaults,
        IUnitOfWork unitOfWork)
    {
        _labs = labs;
        _overrides = overrides;
        _devices = devices;
        _sections = sections;
        _assignments = assignments;
        _users = users;
        _keyHasher = keyHasher;
        _passwords = passwords;
        _importedUserDefaults = importedUserDefaults;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplyScheduleAnalysisResultDto> Handle(ApplyScheduleAnalysisCommand request, CancellationToken cancellationToken)
    {
        var result = new ApplyScheduleAnalysisResultDto();

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var labByName = new Dictionary<string, Lab>(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in await _labs.ListAsync(track: true, cancellationToken: ct).ConfigureAwait(false))
                labByName[existing.Name] = existing;

            var brandNewLabs = new List<Lab>();
            foreach (var proposedLab in request.Request.Labs)
            {
                if (string.IsNullOrWhiteSpace(proposedLab.Name))
                    continue;

                var name = proposedLab.Name.Trim();
                if (labByName.TryGetValue(name, out var existing))
                {
                    result.LabsMatchedExisting++;
                    if (!string.IsNullOrWhiteSpace(proposedLab.CvLabCode))
                        existing.CvLabCode = proposedLab.CvLabCode.Trim();
                    continue;
                }

                var key = string.IsNullOrWhiteSpace(proposedLab.DeviceKey) ? Guid.NewGuid().ToString("N") : proposedLab.DeviceKey.Trim();
                var lab = new Lab
                {
                    Name = name,
                    DeviceKey = key,
                    LightThreshold = 2000,
                    TempFanOnThresholdCelsius = 28f,
                    CvLabCode = string.IsNullOrWhiteSpace(proposedLab.CvLabCode) ? null : proposedLab.CvLabCode.Trim()
                };
                await _labs.AddAsync(lab, ct).ConfigureAwait(false);
                labByName[name] = lab;
                brandNewLabs.Add(lab);
                result.LabsCreated++;
            }

            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            foreach (var lab in brandNewLabs)
            {
                await _overrides.AddAsync(new LabControlOverride { LabId = lab.Id, UpdatedAt = DateTime.UtcNow }, ct).ConfigureAwait(false);
                await _devices.AddAsync(new LabDevice
                {
                    LabId = lab.Id,
                    Name = $"{lab.Name} ESP32",
                    DeviceKeyHash = _keyHasher.Hash(lab.DeviceKey),
                    DeviceType = LabDeviceType.Esp32Controller
                }, ct).ConfigureAwait(false);
            }

            foreach (var section in request.Request.Sections)
            {
                if (!labByName.TryGetValue(section.LabName.Trim(), out var lab))
                {
                    result.Warnings.Add($"Section skipped: unknown lab '{section.LabName}'.");
                    continue;
                }

                await _sections.AddAsync(new LabSectionSchedule
                {
                    LabId = lab.Id,
                    DayOfWeek = section.DayOfWeek,
                    StartTime = section.StartTime,
                    EndTime = section.EndTime,
                    CourseName = section.CourseName
                }, ct).ConfigureAwait(false);
                result.SectionsAdded++;
            }

            foreach (var assignment in request.Request.StaffAssignments)
            {
                if (!labByName.TryGetValue(assignment.LabName.Trim(), out var lab))
                {
                    result.Warnings.Add($"Staff row skipped: unknown lab '{assignment.LabName}'.");
                    continue;
                }

                if (!TryParseRole(assignment.Role, out var role))
                {
                    result.Warnings.Add($"Staff row skipped for '{assignment.PersonName}': invalid role '{assignment.Role}'.");
                    continue;
                }

                var user = await FindOrCreateUserAsync(assignment.PersonName, role, request.Request.CreateMissingUsers, result, ct).ConfigureAwait(false);
                if (user == null)
                {
                    result.Warnings.Add($"Staff row skipped for '{assignment.PersonName}': user not found and CreateMissingUsers is false.");
                    continue;
                }

                await _assignments.AddAsync(new StaffLabAssignment
                {
                    UserId = user.Id,
                    LabId = lab.Id,
                    DayOfWeek = assignment.DayOfWeek,
                    StartTime = assignment.StartTime,
                    EndTime = assignment.EndTime
                }, ct).ConfigureAwait(false);
                result.StaffAssignmentsAdded++;
            }
        }, cancellationToken).ConfigureAwait(false);

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
        var existing = await _users.FindByEmailOrNameAsync(email, trimmed, cancellationToken).ConfigureAwait(false);
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
        user.PasswordHash = _passwords.HashPassword(user, _importedUserDefaults.DefaultPassword);
        await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private static bool TryParseRole(string role, out Role parsedRole)
    {
        parsedRole = Role.TeacherAssistant;
        var value = role.Trim();
        if (string.Equals(value, nameof(Role.Teacher), StringComparison.OrdinalIgnoreCase))
        {
            parsedRole = Role.Teacher;
            return true;
        }

        if (string.Equals(value, nameof(Role.TeacherAssistant), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "TA", StringComparison.OrdinalIgnoreCase)
            || value.Contains("assistant", StringComparison.OrdinalIgnoreCase))
        {
            parsedRole = Role.TeacherAssistant;
            return true;
        }

        return false;
    }
}
