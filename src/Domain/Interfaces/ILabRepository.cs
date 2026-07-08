// Layer: Domain
// Responsibility: Defines lab-specific persistence queries.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for labs and related lab state.
/// </summary>
public interface ILabRepository : IRepository<Lab>
{
    /// <summary>
    /// Gets all labs ordered by name.
    /// </summary>
    Task<IReadOnlyList<Lab>> ListAsync(bool track = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all labs with override and section schedule data.
    /// </summary>
    Task<IReadOnlyList<Lab>> ListWithOverviewDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lab with control override and registered devices.
    /// </summary>
    Task<Lab?> GetWithDeviceStateAsync(int labId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets labs by their identifiers with override and section schedule data.
    /// </summary>
    Task<IReadOnlyList<Lab>> ListCurrentForStaffAsync(IReadOnlyCollection<int> labIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a lab exists.
    /// </summary>
    Task<bool> ExistsAsync(int labId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a lab by name.
    /// </summary>
    Task<Lab?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a lab and dependent rows that would otherwise block cascade delete.
    /// </summary>
    Task DeleteCascadeAsync(int labId, CancellationToken cancellationToken = default);
}
