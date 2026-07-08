// Layer: Domain
// Responsibility: Defines user-specific persistence queries.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for users.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Finds a user by email.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, bool track = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing imported user by email or full name.
    /// </summary>
    Task<User?> FindByEmailOrNameAsync(string email, string fullName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a user role is a staff role.
    /// </summary>
    bool IsStaffRole(Role role);
}
