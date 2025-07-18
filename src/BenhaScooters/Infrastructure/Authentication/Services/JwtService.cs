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

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, Driver? driver = null, Rider? rider = null);
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

    public string GenerateAccessToken(User user, Driver? driver = null, Rider? rider = null)
    {
        var expiresAt = GetAccessTokenExpiryTime();

        // create the token
        List<Claim> claims = [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtClaims.Sub, user.Id.ToString()),
            new Claim(JwtClaims.Name, user.Name),
            new Claim(JwtClaims.Email, user.Email.Value),
            new Claim(JwtClaims.PhoneNumber, user.PhoneNumber.Value),
            new Claim(JwtClaims.EmailVerified, user.EmailVerified.ToString()),
            new Claim(JwtClaims.PhoneVerified, user.PhoneNumberVerified.ToString()),
            new Claim(JwtClaims.Roles, JsonSerializer.Serialize(user.Roles.Select(r => r.Name.ToString()).ToArray()), JsonClaimValueTypes.JsonArray),
        ];

        if (driver is not null)
        {
            claims.Add(new Claim(JwtClaims.DriverId, driver.Id.ToString()));
            claims.Add(new Claim(JwtClaims.OnboardingStatus, driver.OnboardingStatus.ToString()));
        }

        if (rider is not null)
        {
            claims.Add(new Claim(JwtClaims.RiderId, rider.Id.ToString()));
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
