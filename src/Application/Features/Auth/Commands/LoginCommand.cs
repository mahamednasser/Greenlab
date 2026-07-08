// Layer: Application
// Responsibility: Handles user login and token creation.
using FluentValidation;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Domain.Interfaces;
using MediatR;

namespace Greenlab.Application.Features.Auth.Commands;

public record LoginCommand(LoginRequest Request) : IRequest<LoginResponse?>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse?>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHashService _passwords;
    private readonly IJwtTokenService _jwt;

    public LoginCommandHandler(IUserRepository users, IPasswordHashService passwords, IJwtTokenService jwt)
    {
        _users = users;
        _passwords = passwords;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Request.Email, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (user == null || !_passwords.VerifyPassword(user, user.PasswordHash, request.Request.Password))
            return null;

        var token = _jwt.CreateToken(user, out var expiresAt);
        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ExpiresAt = expiresAt
        };
    }
}
