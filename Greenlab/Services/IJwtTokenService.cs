using Greenlab.Models;

namespace Greenlab.Services;

public interface IJwtTokenService
{
    string CreateToken(User user, out DateTime expiresAtUtc);
}
