using System.Security.Claims;
using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Models;
using Greenlab.Options;
using Greenlab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Greenlab.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/schedules")]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly StorageOptions _storage;
    private readonly IScheduleImageAnalysisService _vision;
    private readonly IScheduleAnalysisApplier _applier;

    public SchedulesController(
        AppDbContext db,
        IWebHostEnvironment env,
        IOptions<StorageOptions> storage,
        IScheduleImageAnalysisService vision,
        IScheduleAnalysisApplier applier)
    {
        _db = db;
        _env = env;
        _storage = storage.Value;
        _vision = vision;
        _applier = applier;
    }

    /// <summary>Upload a timetable image for archival / future OCR. Returns import id and stored path.</summary>
    [HttpPost("import-image")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> ImportImage(IFormFile file, [FromForm] string? notes, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File required.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".bin";
        var dir = Path.Combine(_env.ContentRootPath, _storage.ScheduleUploadsPath);
        Directory.CreateDirectory(dir);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, safeName);
        await using (var fs = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }

        var rel = Path.Combine(_storage.ScheduleUploadsPath, safeName).Replace('\\', '/');
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var row = new ScheduleImageImport
        {
            FileName = file.FileName,
            StoredPath = rel,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSizeBytes = file.Length,
            UploadedByUserId = userId,
            Notes = notes
        };
        _db.ScheduleImageImports.Add(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new { row.Id, row.StoredPath, row.FileName };
    }

    /// <summary>Manual / CSV-backed import: creates lab section rows and staff assignments.</summary>
    [HttpPost("import-data")]
    public async Task<ActionResult> ImportData([FromBody] BulkScheduleImportRequest body, CancellationToken cancellationToken)
    {
        foreach (var s in body.LabSections)
        {
            if (!await _db.Labs.AnyAsync(l => l.Id == s.LabId, cancellationToken).ConfigureAwait(false))
                return BadRequest($"Unknown LabId {s.LabId}");
            _db.LabSectionSchedules.Add(new LabSectionSchedule
            {
                LabId = s.LabId,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CourseName = s.CourseName
            });
        }

        foreach (var a in body.StaffAssignments)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == a.StaffEmail, cancellationToken).ConfigureAwait(false);
            if (user == null)
                return BadRequest($"Unknown staff email: {a.StaffEmail}");
            if (user.Role != Role.Teacher && user.Role != Role.TeacherAssistant)
                return BadRequest($"User {a.StaffEmail} is not Teacher or TeacherAssistant.");

            if (!await _db.Labs.AnyAsync(l => l.Id == a.LabId, cancellationToken).ConfigureAwait(false))
                return BadRequest($"Unknown LabId {a.LabId}");

            var hasWeekly = a.DayOfWeek.HasValue && a.StartTime.HasValue && a.EndTime.HasValue;
            var hasRange = a.ValidFrom.HasValue && a.ValidUntil.HasValue;
            if (!hasWeekly && !hasRange)
                return BadRequest("Each staff assignment needs either (DayOfWeek+Start+End) or (ValidFrom+ValidUntil).");

            _db.StaffLabAssignments.Add(new StaffLabAssignment
            {
                UserId = user.Id,
                LabId = a.LabId,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                ValidFrom = a.ValidFrom,
                ValidUntil = a.ValidUntil
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("imports")]
    public async Task<ActionResult<IReadOnlyList<object>>> ListImports(CancellationToken cancellationToken)
    {
        var rows = await _db.ScheduleImageImports
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new { x.Id, x.FileName, x.StoredPath, x.UploadedAt, x.FileSizeBytes })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Cast<object>().ToList();
    }

    /// <summary>Analyze a timetable image with AI (OpenAI). Returns proposed labs/sections/staff — review then call apply-analysis.</summary>
    [HttpPost("analyze-image")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> AnalyzeImage(
        IFormFile file,
        [FromForm] bool persist = true,
        [FromForm] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File required.");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var bytes = ms.ToArray();
        var mime = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;

        int? importId = null;
        if (persist)
        {
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".bin";
            var dir = Path.Combine(_env.ContentRootPath, _storage.ScheduleUploadsPath);
            Directory.CreateDirectory(dir);
            var safeName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, safeName);
            await System.IO.File.WriteAllBytesAsync(fullPath, bytes, cancellationToken).ConfigureAwait(false);

            var rel = Path.Combine(_storage.ScheduleUploadsPath, safeName).Replace('\\', '/');
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var row = new ScheduleImageImport
            {
                FileName = file.FileName,
                StoredPath = rel,
                ContentType = mime,
                FileSizeBytes = file.Length,
                UploadedByUserId = userId,
                Notes = notes
            };
            _db.ScheduleImageImports.Add(row);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            importId = row.Id;
        }

        try
        {
            var analysis = await _vision.AnalyzeImageAsync(bytes, mime, cancellationToken).ConfigureAwait(false);
            return Ok(new { importId, analysis });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Re-run vision on a previously uploaded import row.</summary>
    [HttpPost("analyze-import/{importId:int}")]
    public async Task<ActionResult<ScheduleAnalysisResultDto>> AnalyzeImport(int importId, CancellationToken cancellationToken)
    {
        var row = await _db.ScheduleImageImports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == importId, cancellationToken).ConfigureAwait(false);
        if (row == null) return NotFound();

        var fullPath = Path.Combine(_env.ContentRootPath, row.StoredPath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
            return BadRequest("Stored file is missing on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        try
        {
            return await _vision.AnalyzeImageAsync(bytes, row.ContentType, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Commit labs, section schedules, and staff assignments (optionally creating users).</summary>
    [HttpPost("apply-analysis")]
    public async Task<ActionResult<ApplyScheduleAnalysisResultDto>> ApplyAnalysis([FromBody] ApplyScheduleAnalysisRequest body, CancellationToken cancellationToken)
    {
        var res = await _applier.ApplyAsync(body, cancellationToken).ConfigureAwait(false);
        return res;
    }
}
