// Layer: Application
// Responsibility: Handles schedule import read use cases.
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Schedules.Queries;

public record ScheduleImportListItem(int Id, string FileName, string StoredPath, DateTime UploadedAt, long FileSizeBytes);

public record ListScheduleImportsQuery : IRequest<IReadOnlyList<ScheduleImportListItem>>;

public class ListScheduleImportsQueryHandler : IRequestHandler<ListScheduleImportsQuery, IReadOnlyList<ScheduleImportListItem>>
{
    private readonly IScheduleImageImportRepository _imports;

    public ListScheduleImportsQueryHandler(IScheduleImageImportRepository imports)
    {
        _imports = imports;
    }

    public async Task<IReadOnlyList<ScheduleImportListItem>> Handle(ListScheduleImportsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _imports.ListRecentAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => new ScheduleImportListItem(x.Id, x.FileName, x.StoredPath, x.UploadedAt, x.FileSizeBytes)).ToList();
    }
}
