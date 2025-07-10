using Vogen;

namespace BenhaScooters.Domain;

[ValueObject<Guid>]
public partial struct SmsVerificationCodeId;

public class SmsVerificationCode
{
    public SmsVerificationCodeId Id { get; private set; }
    public UserId UserId { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }
    public string Code { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private SmsVerificationCode() { }

    public SmsVerificationCode(UserId userId, PhoneNumber phoneNumber, string code, TimeSpan expiresIn)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code cannot be empty.", nameof(code));

        Id = SmsVerificationCodeId.From(Guid.NewGuid());
        UserId = userId;
        PhoneNumber = phoneNumber;
        Code = code;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(expiresIn);
        IsUsed = false;
    }

    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;

    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("SMS verification code has already been used.");

        if (DateTime.UtcNow >= ExpiresAt)
            throw new InvalidOperationException("SMS verification code has expired.");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    public static string GenerateCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
