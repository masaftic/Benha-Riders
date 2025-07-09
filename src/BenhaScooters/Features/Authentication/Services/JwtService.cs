using BenhaScooters.Domain;
using FastEndpoints.Security;
using System.Security.Cryptography;

namespace BenhaScooters.Features.Authentication.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiryTime();
    DateTime GetRefreshTokenExpiryTime();
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _signingKey;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _signingKey = _configuration["Jwt:SigningKey"] ?? "The secret used to sign tokens. The secret used to sign tokens.";
    }

    public string GenerateAccessToken(User user)
    {
        var expiresAt = GetAccessTokenExpiryTime();

        return JwtBearer.CreateToken(o =>
        {
            o.SigningKey = _signingKey;
            o.User.Claims.Add(("email", user.Email.Value));
            o.User.Claims.Add(("name", user.Name));
            o.User.Claims.Add(("email_verified", user.EmailVerified.ToString()));
            o.User.Claims.Add(("phone_verified", user.PhoneNumberVerified.ToString()));
            o.User.Roles.AddRange(user.Roles.Select(x => x.Name.ToString()));
            o.User["UserId"] = user.Id.ToString();
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
        var accessTokenLifetime = _configuration.GetValue<int>("Jwt:AccessTokenLifetimeMinutes", 15);
        return DateTime.UtcNow.AddMinutes(accessTokenLifetime);
    }

    public DateTime GetRefreshTokenExpiryTime()
    {
        var refreshTokenLifetime = _configuration.GetValue<int>("Jwt:RefreshTokenLifetimeDays", 7);
        return DateTime.UtcNow.AddDays(refreshTokenLifetime);
    }
}
