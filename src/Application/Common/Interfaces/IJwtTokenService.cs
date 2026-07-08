// Layer: Application
// Responsibility: Defines the contract for issuing authentication tokens.
using Greenlab.Domain.Entities;

namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Creates JWT access tokens for authenticated users.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Creates a token and returns its expiration instant.
    /// </summary>
    string CreateToken(User user, out DateTime expiresAtUtc);
}
