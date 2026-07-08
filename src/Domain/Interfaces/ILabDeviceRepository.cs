// Layer: Domain
// Responsibility: Defines persistence operations for lab devices.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for lab devices.
/// </summary>
public interface ILabDeviceRepository : IRepository<LabDevice>
{
    /// <summary>
    /// Lists devices for a lab, ordered by name.
    /// </summary>
    Task<IReadOnlyList<LabDevice>> ListByLabIdAsync(int labId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device that belongs to the given lab.
    /// </summary>
    Task<LabDevice?> GetByLabAndDeviceIdAsync(int labId, int deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device read-only (no tracking).
    /// </summary>
    Task<LabDevice?> GetByLabAndDeviceIdNoTrackingAsync(int labId, int deviceId, CancellationToken cancellationToken = default);
}
