namespace BenhaScooters.Infrastructure.Authentication.Services;

/// <summary>
/// Configuration for OTP security thresholds.
/// Bind to "OtpSecurity" section in appsettings.json.
/// </summary>
public class OtpSecurityOptions
{
    public const string SectionName = "OtpSecurity";

    // ─── Rate Limits (sending) ───────────────────────────────────────

    /// <summary>Minimum seconds between consecutive OTP requests for the same user.</summary>
    public int CooldownSeconds { get; set; } = 60;

    /// <summary>Maximum OTP requests per user per hour.</summary>
    public int MaxRequestsPerUserPerHour { get; set; } = 5;

    /// <summary>Maximum OTP requests per user per day.</summary>
    public int MaxRequestsPerUserPerDay { get; set; } = 10;

    /// <summary>Maximum OTP requests per IP per hour.</summary>
    public int MaxRequestsPerIpPerHour { get; set; } = 15;

    /// <summary>Maximum OTP requests per IP per day.</summary>
    public int MaxRequestsPerIpPerDay { get; set; } = 30;

    /// <summary>Maximum OTP requests per phone number per hour (across all users/IPs).</summary>
    public int MaxRequestsPerPhonePerHour { get; set; } = 5;

    /// <summary>Maximum OTP requests per phone number per day.</summary>
    public int MaxRequestsPerPhonePerDay { get; set; } = 10;

    // ─── Progressive Cooldown ────────────────────────────────────────

    /// <summary>
    /// Progressive cooldown tiers in seconds.
    /// Applied based on how many requests were made in the current hour.
    /// Index 0 = after 1st request, Index 1 = after 2nd, etc.
    /// </summary>
    public int[] ProgressiveCooldownSeconds { get; set; } = [60, 120, 300, 600, 1800];

    // ─── Verification Limits ─────────────────────────────────────────

    /// <summary>Maximum failed verification attempts per code before it's locked.</summary>
    public int MaxFailedAttemptsPerCode { get; set; } = 5;

    /// <summary>Maximum total failed verification attempts per user per hour before temporary lockout.</summary>
    public int MaxFailedAttemptsPerUserPerHour { get; set; } = 15;

    /// <summary>Lockout duration in seconds after too many failed attempts.</summary>
    public int VerificationLockoutSeconds { get; set; } = 1800; // 30 minutes

    // ─── Code Configuration ──────────────────────────────────────────

    /// <summary>OTP code expiry in minutes.</summary>
    public int CodeExpiryMinutes { get; set; } = 5;

    /// <summary>HMAC key for hashing codes. MUST be set in production.</summary>
    public string HmacKey { get; set; } = "CHANGE_ME_IN_PRODUCTION_USE_64_CHAR_HEX";

    /// <summary>Use fixed OTP code for testing (disables random code generation).</summary>
    public bool UseFixedOtp { get; set; } = false;

    /// <summary>Fixed OTP code to use when UseFixedOtp is true.</summary>
    public string FixedOtpCode { get; set; } = "123456";

    // ─── Fraud Detection ─────────────────────────────────────────────

    /// <summary>Number of distinct phone numbers a single IP can request codes for per hour before flagging.</summary>
    public int MaxDistinctPhonesPerIpPerHour { get; set; } = 5;

    /// <summary>Number of distinct IPs a single user can send OTPs from per day before flagging.</summary>
    public int MaxDistinctIpsPerUserPerDay { get; set; } = 5;
}
