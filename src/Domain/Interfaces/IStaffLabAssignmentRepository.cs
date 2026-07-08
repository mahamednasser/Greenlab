// Layer: Domain
// Responsibility: Defines persistence operations for staff lab assignments.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for staff lab assignments.
/// </summary>
public interface IStaffLabAssignmentRepository : IRepository<StaffLabAssignment>
{
    /// <summary>
    /// Lists all assignments for a staff user.
    /// </summary>
    Task<IReadOnlyList<StaffLabAssignment>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);
}
