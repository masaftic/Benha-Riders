using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record RefreshTokenCommand(string RefreshToken, App App) : IRequest<ErrorOr<RefreshTokenResponse>>;

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
            return AppErrors.User.InvalidRefreshToken();
        }

        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return AppErrors.User.NotFound();
        }

        if (!user.IsActive)
        {
            return AppErrors.User.AccountDeactivated();
        }

        // Load or create profiles based on app
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

        var riderProfile = await _db.RiderProfiles
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        // Create profile if it doesn't exist for the requested app
        if (request.App == App.DriverApp && driverProfile is null)
        {
            driverProfile = new DriverProfile(user.Id);
            _db.DriverProfiles.Add(driverProfile);
        }
        else if (request.App == App.RiderApp && riderProfile is null)
        {
            riderProfile = new RiderProfile(user.Id, user.Name);
            _db.RiderProfiles.Add(riderProfile);
        }

        // Revoke the old refresh token
        refreshToken.Revoke();

        // Generate new tokens using the authentication service
        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, request.App, driverProfile, riderProfile, cancellationToken);

        return new RefreshTokenResponse(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt);
    }
}
