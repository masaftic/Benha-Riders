using Thinktecture;

namespace BenhaScooters.Domain.Users;

[ValueObject<int>]
public partial struct OtpSecurityEventId;

public enum OtpEventType
{
    CodeRequested = 0,
    CodeVerified = 1,
    CodeFailed = 2,
    CodeExpired = 3,
    CodeLocked = 4,
    RateLimited = 5,
    FraudSuspected = 6,
    PreviousCodesInvalidated = 7,
}

/// <summary>
/// Immutable audit log for all OTP-related security events.
/// Used for fraud detection, rate limit analysis, and compliance auditing.
/// </summary>
public class OtpSecurityEvent
{
    public OtpSecurityEventId Id { get; private set; }
    public UserId? UserId { get; private set; }
    public string PhoneNumber { get; private set; } = null!;
    public string? IpAddress { get; private set; }
    public OtpEventType EventType { get; private set; }

    /// <summary>Optional JSON metadata (e.g., fraud score, reason, user-agent fingerprint).</summary>
    public string? Metadata { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private OtpSecurityEvent() { }

    public OtpSecurityEvent(UserId? userId, string phoneNumber, string? ipAddress, OtpEventType eventType, string? metadata = null)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
        IpAddress = ipAddress;
        EventType = eventType;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
    }
}
