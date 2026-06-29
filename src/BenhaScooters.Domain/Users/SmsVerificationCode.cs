using System.Security.Cryptography;
using System.Text;
using Thinktecture;


namespace BenhaScooters.Domain.Users;

[ValueObject<int>]
public partial struct SmsVerificationCodeId;

public class SmsVerificationCode
{
    /// <summary>Max verification attempts before the code is permanently locked.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Default OTP lifetime.</summary>
    public static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    public SmsVerificationCodeId Id { get; private set; }
    public UserId UserId { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; } = null!;

    /// <summary>HMAC-SHA256 hash of the code (never store plaintext in production).</summary>
    public string CodeHash { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    /// <summary>Number of failed verification attempts against this code.</summary>
    public int FailedAttempts { get; private set; }

    /// <summary>Timestamp when the code was locked due to too many failed attempts.</summary>
    public DateTime? LockedAt { get; private set; }

    /// <summary>IP address that requested this code (for fraud analysis).</summary>
    public string? IpAddress { get; private set; }

    public bool IsLocked => LockedAt.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsLocked && !IsExpired;

    private SmsVerificationCode() { }

    public SmsVerificationCode(UserId userId, PhoneNumber phoneNumber, string codeHash, TimeSpan expiresIn, string? ipAddress = null)
    {
        if (string.IsNullOrEmpty(codeHash))
            throw new ArgumentException("Code hash cannot be empty.", nameof(codeHash));

        UserId = userId;
        PhoneNumber = phoneNumber;
        CodeHash = codeHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(expiresIn);
        IsUsed = false;
        FailedAttempts = 0;
        IpAddress = ipAddress;
    }

    /// <summary>
    /// Record a failed verification attempt. Locks the code if max attempts exceeded.
    /// Returns true if the code was just locked.
    /// </summary>
    public bool RecordFailedAttempt()
    {
        if (IsLocked) return false;

        FailedAttempts++;
        if (FailedAttempts >= MaxFailedAttempts)
        {
            LockedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public int RemainingAttempts => Math.Max(0, MaxFailedAttempts - FailedAttempts);

    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("SMS verification code has already been used.");

        if (IsLocked)
            throw new InvalidOperationException("SMS verification code has been locked.");

        if (IsExpired)
            throw new InvalidOperationException("SMS verification code has expired.");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Invalidate all active codes for a user (called when a new code is issued).
    /// </summary>
    public void Invalidate()
    {
        if (!IsUsed && !IsLocked)
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
        }
    }

    // ----- Cryptographic helpers -----

    /// <summary>
    /// Generate a cryptographically secure 6-digit OTP code.
    /// </summary>
    public static string GenerateCode()
    {
        // Use rejection sampling with crypto RNG for uniform distribution
        Span<byte> bytes = stackalloc byte[4];
        int code;
        do
        {
            RandomNumberGenerator.Fill(bytes);
            code = (int)(BitConverter.ToUInt32(bytes) % 1_000_000);
        } while (code < 100_000); // Ensure 6 digits

        return code.ToString("D6");
    }

    /// <summary>
    /// Compute HMAC-SHA256 hash of a code using the provided key.
    /// </summary>
    public static string HashCode(string code, byte[] hmacKey)
    {
        using var hmac = new HMACSHA256(hmacKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Constant-time comparison of two code hashes to prevent timing attacks.
    /// </summary>
    public static bool VerifyCodeHash(string code, string storedHash, byte[] hmacKey)
    {
        var computedHash = HashCode(code, hmacKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
