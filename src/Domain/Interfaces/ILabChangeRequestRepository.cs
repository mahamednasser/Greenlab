// Layer: Domain
// Responsibility: Defines persistence operations for lab change requests.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for lab change requests.
/// </summary>
public interface ILabChangeRequestRepository : IRepository<LabChangeRequest>
{
    /// <summary>
    /// Lists lab change requests, optionally filtered by status.
    /// </summary>
    Task<IReadOnlyList<LabChangeRequest>> ListAsync(LabChangeRequestStatus? status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists lab change requests created by a staff user.
    /// </summary>
    Task<IReadOnlyList<LabChangeRequest>> ListByRequesterAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lab change request with review data.
    /// </summary>
    Task<LabChangeRequest?> GetForReviewAsync(int id, CancellationToken cancellationToken = default);
}
