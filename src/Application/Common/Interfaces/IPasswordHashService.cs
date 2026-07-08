// Layer: Application
// Responsibility: Defines password hashing and verification operations.
using Greenlab.Domain.Entities;

namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies user passwords.
/// </summary>
public interface IPasswordHashService
{
    /// <summary>
    /// Hashes a password for the supplied user.
    /// </summary>
    string HashPassword(User user, string password);

    /// <summary>
    /// Verifies a password against an existing hash.
    /// </summary>
    bool VerifyPassword(User user, string passwordHash, string password);
}
