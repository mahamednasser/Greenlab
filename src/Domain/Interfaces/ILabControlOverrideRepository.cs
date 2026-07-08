// Layer: Domain
// Responsibility: Defines persistence operations for lab control overrides.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for lab control overrides.
/// </summary>
public interface ILabControlOverrideRepository : IRepository<LabControlOverride>
{
    /// <summary>
    /// Gets the override row for a lab.
    /// </summary>
    Task<LabControlOverride?> GetByLabIdAsync(int labId, CancellationToken cancellationToken = default);
}
