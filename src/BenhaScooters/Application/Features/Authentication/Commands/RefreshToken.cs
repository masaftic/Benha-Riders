using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ErrorOr<RefreshTokenResponse>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ErrorOr<RefreshTokenResponse>>
{
    private readonly AppDbContext _db;
    private readonly IAuthenticationService _authenticationService;

    public RefreshTokenCommandHandler(AppDbContext db, IAuthenticationService authenticationService)
    {
        _db = db;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            return UserErrors.InvalidRefreshToken;
        }

        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

        var riderProfile = await _db.RiderProfiles
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        // Revoke the old refresh token
        refreshToken.Revoke();

        // Generate new tokens using the authentication service
        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, driverProfile, riderProfile, cancellationToken);

        return new RefreshTokenResponse(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt);
    }
}
