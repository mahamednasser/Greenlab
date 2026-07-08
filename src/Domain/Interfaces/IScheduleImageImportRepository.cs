// Layer: Domain
// Responsibility: Defines persistence operations for schedule image imports.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for uploaded schedule image imports.
/// </summary>
public interface IScheduleImageImportRepository : IRepository<ScheduleImageImport>
{
    /// <summary>
    /// Lists schedule image imports ordered by most recent first.
    /// </summary>
    Task<IReadOnlyList<ScheduleImageImport>> ListRecentAsync(CancellationToken cancellationToken = default);
}
