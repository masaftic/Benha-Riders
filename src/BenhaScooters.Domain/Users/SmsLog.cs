using Thinktecture;

namespace BenhaScooters.Domain.Users;

[ValueObject<int>]
public partial struct SmsLogId;

public enum SmsStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2,
    Rejected = 3
}

public enum SmsType
{
    Verification = 0,
    General = 1,
    Notification = 2
}

/// <summary>
/// Audit log for all SMS sends via WhySMS API.
/// Stores provider response data for debugging and compliance.
/// </summary>
public class SmsLog
{
    public SmsLogId Id { get; private set; }
    public UserId? UserId { get; private set; }
    public string PhoneNumber { get; private set; } = null!;
    public SmsType Type { get; private set; }
    public string Message { get; private set; } = null!;
    
    /// <summary>SMS provider's message ID (for tracking).</summary>
    public string? ProviderId { get; private set; }
    
    /// <summary>Provider's unique ID for this message.</summary>
    public string? ProviderUid { get; private set; }
    
    public SmsStatus Status { get; private set; }
    public int Cost { get; private set; }
    public int SmsCount { get; private set; }
    
    /// <summary>Error message if sending failed.</summary>
    public string? ErrorMessage { get; private set; }
    
    /// <summary>IP address that triggered this SMS (for fraud tracking).</summary>
    public string? IpAddress { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    private SmsLog() { }

    public SmsLog(
        UserId? userId,
        string phoneNumber,
        SmsType type,
        string message,
        string? ipAddress = null)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
        Type = type;
        Message = message;
        IpAddress = ipAddress;
        Status = SmsStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkDelivered(string providerId, string providerUid, int cost, int smsCount)
    {
        ProviderId = providerId;
        ProviderUid = providerUid;
        Status = SmsStatus.Delivered;
        Cost = cost;
        SmsCount = smsCount;
        DeliveredAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = SmsStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void MarkRejected(string errorMessage)
    {
        Status = SmsStatus.Rejected;
        ErrorMessage = errorMessage;
    }
}
