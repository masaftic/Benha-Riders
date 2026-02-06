using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Shared.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, DriverProfile? driverProfile = null, RiderProfile? riderProfile = null);
    string GenerateOnboardingToken(UserId userId, UserStatus userStatus, string nextStep);
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

    public string GenerateAccessToken(User user, DriverProfile? driverProfile = null, RiderProfile? riderProfile = null)
    {
        var expiresAt = GetAccessTokenExpiryTime();

        // create the token
        List<Claim> claims = [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtClaims.Sub, user.Id.ToString()),
            new Claim(JwtClaims.Name, user.Name),
            new Claim(JwtClaims.Email, user.Email),
            new Claim(JwtClaims.PhoneNumber, user.PhoneNumber ?? ""),
            new Claim(JwtClaims.Status, user.Status.ToString()),
            new Claim(JwtClaims.EmailVerified, user.EmailVerified.ToString()),
            new Claim(JwtClaims.PhoneVerified, user.PhoneNumberVerified.ToString()),
            new Claim(JwtClaims.Roles, JsonSerializer.Serialize(user.Roles.Select(r => r.Name.ToString()).ToArray()), JsonClaimValueTypes.JsonArray),
        ];

        if (driverProfile is not null)
        {
            claims.Add(new Claim(JwtClaims.DriverOnboardingStatus, driverProfile.OnboardingStatus.ToString()));
        }

        if (riderProfile is not null)
        {
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateOnboardingToken(UserId userId, UserStatus userStatus, string nextStep)
    {
        var claims = new List<Claim>
        {
            new(JwtClaims.Sub, userId.ToString()),
            new(JwtClaims.Status, userStatus.ToString()), 
            new(JwtClaims.NextStep, nextStep)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // Smaller window for the user onboarding token
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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
