using BenhaScooters.Domain;
using BenhaScooters.Shared.Security;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiryTime();
    DateTime GetRefreshTokenExpiryTime();
}

public class JwtService : IJwtService
{
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly string _signingKey;

    public JwtService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions;
        _signingKey = _jwtOptions.Value.SigningKey;
    }

    public string GenerateAccessToken(User user)
    {
        var expiresAt = GetAccessTokenExpiryTime();

        return JwtBearer.CreateToken(o =>
        {
            o.SigningKey = _signingKey;
            o.User.Claims.Add((JwtClaims.Sub, user.Id.ToString()));
            o.User.Claims.Add((JwtClaims.Email, user.Email.Value));
            o.User.Claims.Add((JwtClaims.Name, user.Name));
            o.User.Claims.Add((JwtClaims.EmailVerified, user.EmailVerified.ToString()));
            o.User.Claims.Add((JwtClaims.PhoneVerified, user.PhoneNumberVerified.ToString()));
            o.User.Roles.AddRange(user.Roles.Select(x => x.Name.ToString()));
            o.ExpireAt = expiresAt;
        });
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public DateTime GetAccessTokenExpiryTime()
    {
        var accessTokenLifetime = _jwtOptions.Value.AccessTokenLifetimeMinutes;
        return DateTime.UtcNow.AddMinutes(accessTokenLifetime);
    }

    public DateTime GetRefreshTokenExpiryTime()
    {
        var refreshTokenLifetime = _jwtOptions.Value.RefreshTokenLifetimeDays;
        return DateTime.UtcNow.AddDays(refreshTokenLifetime);
    }
}
