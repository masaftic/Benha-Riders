using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;

namespace BenhaScooters.Application.Services;

public interface IAuthenticationService
{
    Task<AuthenticatedResponse> GenerateAuthenticatedResponseAsync(
        User user, 
        DriverProfile? driverProfile = null, 
        RiderProfile? riderProfile = null, 
        CancellationToken cancellationToken = default);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthenticationService(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<AuthenticatedResponse> GenerateAuthenticatedResponseAsync(
        User user, 
        DriverProfile? driverProfile = null, 
        RiderProfile? riderProfile = null, 
        CancellationToken cancellationToken = default)
    {
        var expiresAt = _jwtService.GetAccessTokenExpiryTime();
        var accessToken = _jwtService.GenerateAccessToken(user, driverProfile, riderProfile);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Create and store refresh token
        var refreshTokenEntity = user.CreateRefreshToken(refreshToken, _jwtService.GetRefreshTokenExpiryTime());
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthenticatedResponse(accessToken, refreshToken, expiresAt);
    }
}
