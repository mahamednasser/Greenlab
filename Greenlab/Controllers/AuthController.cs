using Greenlab.Data;
using Greenlab.DTOs;
using Greenlab.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly PasswordHasher<Models.User> _hasher = new();

    public AuthController(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == req.Email, cancellationToken).ConfigureAwait(false);
        if (user == null)
            return Unauthorized();

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();

        var token = _jwt.CreateToken(user, out var exp);
        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ExpiresAt = exp
        };
    }
}
