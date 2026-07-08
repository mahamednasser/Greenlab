// Layer: Infrastructure
// Responsibility: Hashes and verifies passwords using ASP.NET Core Identity primitives.
using Greenlab.Application.Common.Interfaces;
using Greenlab.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Greenlab.Infrastructure.Services;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string passwordHash, string password)
    {
        return _hasher.VerifyHashedPassword(user, passwordHash, password) != PasswordVerificationResult.Failed;
    }
}
