// Layer: Domain
// Responsibility: Defines persistence operations for lab section schedules.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for lab section schedules.
/// </summary>
public interface ILabSectionScheduleRepository : IRepository<LabSectionSchedule>
{
    /// <summary>
    /// Determines whether a lab has an active section overlapping the supplied window.
    /// </summary>
    Task<bool> HasOverlapAsync(int labId, DateTime sectionStart, DateTime sectionEnd, CancellationToken cancellationToken = default);
}
