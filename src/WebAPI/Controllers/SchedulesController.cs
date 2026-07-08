// Layer: WebAPI
// Responsibility: Exposes schedule import and analysis endpoints and delegates use cases to MediatR.
using System.Security.Claims;
using Greenlab.Application.DTOs;
using Greenlab.Application.Features.Schedules.Commands;
using Greenlab.Application.Features.Schedules.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Greenlab.WebAPI.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/schedules")]
public class SchedulesController : ControllerBase
{
    private readonly ISender _sender;

    public SchedulesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("import-image")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> ImportImage(IFormFile file, [FromForm] string? notes, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File required.");

        var bytes = await ReadBytesAsync(file, cancellationToken).ConfigureAwait(false);
        var result = await _sender.Send(new ImportScheduleImageCommand(file.FileName, file.ContentType, file.Length, bytes, GetUserId(), notes), cancellationToken).ConfigureAwait(false);
        return new { result.Id, result.StoredPath, result.FileName };
    }

    [HttpPost("import-data")]
    public async Task<IActionResult> ImportData([FromBody] BulkScheduleImportRequest body, CancellationToken cancellationToken)
    {
        await _sender.Send(new ImportScheduleDataCommand(body), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("imports")]
    public async Task<ActionResult<IReadOnlyList<ScheduleImportListItem>>> ListImports(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new ListScheduleImportsQuery(), cancellationToken).ConfigureAwait(false));
    }

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

        var bytes = await ReadBytesAsync(file, cancellationToken).ConfigureAwait(false);
        var result = await _sender.Send(new AnalyzeScheduleImageCommand(file.FileName, file.ContentType, file.Length, bytes, persist, GetUserId(), notes), cancellationToken).ConfigureAwait(false);
        return Ok(new { importId = result.ImportId, analysis = result.Analysis });
    }

    [HttpPost("analyze-import/{importId:int}")]
    public async Task<ActionResult<ScheduleAnalysisResultDto>> AnalyzeImport(int importId, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new AnalyzeScheduleImportCommand(importId), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("apply-analysis")]
    public async Task<ActionResult<ApplyScheduleAnalysisResultDto>> ApplyAnalysis([FromBody] ApplyScheduleAnalysisRequest body, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new ApplyScheduleAnalysisCommand(body), cancellationToken).ConfigureAwait(false));
    }

    private static async Task<byte[]> ReadBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        return stream.ToArray();
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
