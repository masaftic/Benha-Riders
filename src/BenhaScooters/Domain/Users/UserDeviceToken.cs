using Thinktecture;

namespace BenhaScooters.Domain.Users;

[ValueObject<int>]
public partial struct UserDeviceTokenId;

public class UserDeviceToken
{
    public UserDeviceTokenId Id { get; private set; }
    public UserId UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public string Platform { get; private set; } = null!; // "android" or "ios"
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUsedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private UserDeviceToken() { } // EF Core

    public UserDeviceToken(UserId userId, string token, string platform)
    {
        UserId = userId;
        Token = token;
        Platform = platform.ToLowerInvariant();
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void UpdateToken(string newToken)
    {
        Token = newToken;
        LastUsedAt = DateTime.UtcNow;
    }
}
