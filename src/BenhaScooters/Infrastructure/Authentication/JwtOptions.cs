using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    [Required]
    [MinLength(16, ErrorMessage = "Signing key must be at least 16 characters long.")]
    public string SigningKey { get; set; } = null!;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;

    [Required]
    public string Issuer { get; set; } = null!;
    [Required]
    public string Audience { get; set; } = null!;
}
