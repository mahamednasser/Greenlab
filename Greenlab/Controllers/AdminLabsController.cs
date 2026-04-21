using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/labs")]
public class AdminLabsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminLabsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LabDetailDto>>> List(CancellationToken cancellationToken)
    {
        var rows = await _db.Labs.AsNoTracking().OrderBy(l => l.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(MapDetail).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LabDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
        if (lab == null) return NotFound();
        return MapDetail(lab);
    }

    [HttpPost]
    public async Task<ActionResult<LabDetailDto>> Create([FromBody] CreateLabRequest req, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(req.DeviceKey) ? Guid.NewGuid().ToString("N") : req.DeviceKey.Trim();
        var lab = new Lab
        {
            Name = req.Name.Trim(),
            DeviceKey = key,
            LightThreshold = req.LightThreshold,
            TempFanOnThresholdCelsius = req.TempFanOnThresholdCelsius,
            CvLabCode = string.IsNullOrWhiteSpace(req.CvLabCode) ? null : req.CvLabCode.Trim(),
            CameraRtspUrl = string.IsNullOrWhiteSpace(req.CameraRtspUrl) ? null : req.CameraRtspUrl.Trim()
        };
        _db.Labs.Add(lab);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _db.LabControlOverrides.Add(new LabControlOverride { LabId = lab.Id, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(Get), new { id = lab.Id }, MapDetail(lab));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LabDetailDto>> Update(int id, [FromBody] UpdateLabRequest req, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
        if (lab == null) return NotFound();

        lab.Name = req.Name.Trim();
        lab.DeviceKey = req.DeviceKey.Trim();
        lab.LightThreshold = req.LightThreshold;
        lab.TempFanOnThresholdCelsius = req.TempFanOnThresholdCelsius;
        lab.CvLabCode = string.IsNullOrWhiteSpace(req.CvLabCode) ? null : req.CvLabCode.Trim();
        lab.CameraRtspUrl = string.IsNullOrWhiteSpace(req.CameraRtspUrl) ? null : req.CameraRtspUrl.Trim();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(lab);
    }

    /// <summary>Add section time slots to a lab (weekly recurring).</summary>
    [HttpPost("{id:int}/sections")]
    public async Task<ActionResult> AddSections(int id, [FromBody] AddLabSectionsRequest req, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken).ConfigureAwait(false);
        if (lab == null) return NotFound();

        foreach (var s in req.Sections)
        {
            _db.LabSectionSchedules.Add(new LabSectionSchedule
            {
                LabId = id,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CourseName = s.CourseName
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    private static LabDetailDto MapDetail(Lab l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        DeviceKey = l.DeviceKey,
        LightThreshold = l.LightThreshold,
        TempFanOnThresholdCelsius = l.TempFanOnThresholdCelsius,
        CvLabCode = l.CvLabCode,
        CameraRtspUrl = l.CameraRtspUrl
    };
}
