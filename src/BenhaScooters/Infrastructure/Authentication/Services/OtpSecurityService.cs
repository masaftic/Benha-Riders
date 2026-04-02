using System.Text;
using System.Text.Json;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.Authentication.Services;

/// <summary>
/// Orchestrates secure OTP generation, verification, rate limiting, and fraud detection.
/// This is the single entry point for all OTP operations.
/// </summary>
public interface IOtpSecurityService
{
    /// <summary>
    /// Request a new OTP code. Performs rate limiting, fraud detection, invalidates previous codes,
    /// generates a cryptographically secure code, hashes and stores it.
    /// Returns the plaintext code to be sent via SMS.
    /// </summary>
    Task<ErrorOr<OtpSendResult>> RequestCodeAsync(
        UserId userId, PhoneNumber phoneNumber, string? ipAddress, CancellationToken ct);

    /// <summary>
    /// Verify an OTP code. Performs brute-force protection, constant-time comparison,
    /// and records security events.
    /// </summary>
    Task<ErrorOr<Success>> VerifyCodeAsync(
        UserId userId, string code, string? ipAddress, CancellationToken ct);
}

public record OtpSendResult(string Code, int NextCooldownSeconds);

public class OtpSecurityService : IOtpSecurityService
{
    private readonly AppDbContext _db;
    private readonly OtpRateLimiter _rateLimiter;
    private readonly IOtpFraudDetector _fraudDetector;
    private readonly OtpSecurityOptions _options;
    private readonly byte[] _hmacKey;
    private readonly ILogger<OtpSecurityService> _logger;

    public OtpSecurityService(
        AppDbContext db,
        OtpRateLimiter rateLimiter,
        IOtpFraudDetector fraudDetector,
        IOptions<OtpSecurityOptions> options,
        ILogger<OtpSecurityService> logger)
    {
        _db = db;
        _rateLimiter = rateLimiter;
        _fraudDetector = fraudDetector;
        _options = options.Value;
        _hmacKey = Encoding.UTF8.GetBytes(_options.HmacKey);
        _logger = logger;
    }

    public async Task<ErrorOr<OtpSendResult>> RequestCodeAsync(
        UserId userId, PhoneNumber phoneNumber, string? ipAddress, CancellationToken ct)
    {
        var phoneStr = phoneNumber;

        // ─── 1. Fraud Detection ──────────────────────────────────────
        var fraudResult = await _fraudDetector.AnalyzeAsync(userId, phoneStr, ipAddress, ct);
        if (fraudResult.IsBlocked)
        {
            await LogEventAsync(userId, phoneStr, ipAddress, OtpEventType.FraudSuspected,
                JsonSerializer.Serialize(new { fraudResult.Score, fraudResult.Signals }), ct);

            _logger.LogCritical(
                "OTP request BLOCKED due to fraud. UserId={UserId}, Phone={Phone}, IP={Ip}, Score={Score}",
                userId, phoneStr, ipAddress, fraudResult.Score);

            return AppErrors.User.OtpFraudSuspected();
        }

        // ─── 2. Rate Limiting ────────────────────────────────────────
        var rateLimitCheck = await _rateLimiter.CheckSendRateLimitAsync(userId, phoneStr, ipAddress, ct);
        if (rateLimitCheck.IsError)
        {
            await LogEventAsync(userId, phoneStr, ipAddress, OtpEventType.RateLimited,
                JsonSerializer.Serialize(new { Errors = rateLimitCheck.Errors.Select(e => e.Code) }), ct);
            return rateLimitCheck.Errors;
        }

        // ─── 3. Invalidate Previous Active Codes ─────────────────────
        var activeCodes = await _db.SmsVerificationCodes
            .Where(x => x.UserId == userId && !x.IsUsed && !x.LockedAt.HasValue && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        if (activeCodes.Count > 0)
        {
            foreach (var activeCode in activeCodes)
            {
                activeCode.Invalidate();
            }

            await LogEventAsync(userId, phoneStr, ipAddress, OtpEventType.PreviousCodesInvalidated,
                JsonSerializer.Serialize(new { InvalidatedCount = activeCodes.Count }), ct);
        }

        // ─── 4. Generate Secure Code ─────────────────────────────────
        var plainCode = _options.UseFixedOtp 
            ? _options.FixedOtpCode 
            : SmsVerificationCode.GenerateCode();
        var codeHash = SmsVerificationCode.HashCode(plainCode, _hmacKey);

        var verificationCode = new SmsVerificationCode(
            userId,
            phoneNumber,
            codeHash,
            TimeSpan.FromMinutes(_options.CodeExpiryMinutes),
            ipAddress);

        _db.SmsVerificationCodes.Add(verificationCode);

        // ─── 5. Log Event ────────────────────────────────────────────
        await LogEventAsync(userId, phoneStr, ipAddress, OtpEventType.CodeRequested,
            fraudResult.IsSuspicious
                ? JsonSerializer.Serialize(new { FraudScore = fraudResult.Score, fraudResult.Signals })
                : null,
            ct);

        await _db.SaveChangesAsync(ct);

        // ─── 6. Update Rate Limit Counters ───────────────────────────
        _rateLimiter.RecordSend(userId, phoneStr, ipAddress);

        _logger.LogInformation("OTP code generated for UserId={UserId}, Phone={Phone}", userId, phoneStr);

        return new OtpSendResult(plainCode, rateLimitCheck.Value.CooldownSeconds);
    }

    public async Task<ErrorOr<Success>> VerifyCodeAsync(
        UserId userId, string code, string? ipAddress, CancellationToken ct)
    {
        // ─── 1. Check Verification Rate Limit ────────────────────────
        var rateLimitCheck = await _rateLimiter.CheckVerifyRateLimitAsync(userId, ipAddress, ct);
        if (rateLimitCheck.IsError)
        {
            await LogEventAsync(userId, "", ipAddress, OtpEventType.RateLimited,
                JsonSerializer.Serialize(new { Errors = rateLimitCheck.Errors.Select(e => e.Code) }), ct);
            return rateLimitCheck.Errors;
        }

        // ─── 2. Find Active Verification Code ───────────────────────
        var verificationCode = await _db.SmsVerificationCodes
            .Where(x => x.UserId == userId && !x.IsUsed && !x.LockedAt.HasValue && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (verificationCode is null)
        {
            // No active code found — could be expired or never sent
            _rateLimiter.RecordVerificationFailure(userId);
            await LogEventAsync(userId, "", ipAddress, OtpEventType.CodeFailed,
                JsonSerializer.Serialize(new { Reason = "No active code found" }), ct);
            await _db.SaveChangesAsync(ct);
            return AppErrors.User.InvalidVerificationCode();
        }

        // ─── 3. Check if Code is Locked ──────────────────────────────
        if (verificationCode.IsLocked)
        {
            await LogEventAsync(userId, verificationCode.PhoneNumber, ipAddress, OtpEventType.CodeFailed,
                JsonSerializer.Serialize(new { Reason = "Code is locked" }), ct);
            await _db.SaveChangesAsync(ct);
            return AppErrors.User.OtpCodeLocked();
        }

        // ─── 4. Constant-Time Code Verification ─────────────────────
        var isValid = SmsVerificationCode.VerifyCodeHash(code, verificationCode.CodeHash, _hmacKey);

        if (!isValid)
        {
            var wasLocked = verificationCode.RecordFailedAttempt();
            _rateLimiter.RecordVerificationFailure(userId);

            var metadata = JsonSerializer.Serialize(new
            {
                Reason = "Invalid code",
                verificationCode.FailedAttempts,
                verificationCode.RemainingAttempts,
                WasLocked = wasLocked
            });

            await LogEventAsync(userId, verificationCode.PhoneNumber, ipAddress,
                wasLocked ? OtpEventType.CodeLocked : OtpEventType.CodeFailed, metadata, ct);

            await _db.SaveChangesAsync(ct);

            if (wasLocked)
            {
                _logger.LogWarning("OTP code locked for UserId={UserId} after {Attempts} failed attempts",
                    userId, verificationCode.FailedAttempts);
                return AppErrors.User.OtpCodeLocked();
            }

            _logger.LogInformation("OTP verification failed for UserId={UserId}. {Remaining} attempts remaining",
                userId, verificationCode.RemainingAttempts);
            return AppErrors.User.InvalidVerificationCode();
        }

        // ─── 5. Mark as Used ─────────────────────────────────────────
        verificationCode.MarkAsUsed();

        await LogEventAsync(userId, verificationCode.PhoneNumber, ipAddress, OtpEventType.CodeVerified, null, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("OTP verified successfully for UserId={UserId}", userId);

        return Result.Success;
    }

    // ─── Private Helpers ──────────────────────────────────────────────

    private async Task LogEventAsync(
        UserId? userId, string phoneNumber, string? ipAddress,
        OtpEventType eventType, string? metadata, CancellationToken ct)
    {
        var evt = new OtpSecurityEvent(userId, phoneNumber, ipAddress, eventType, metadata);
        _db.OtpSecurityEvents.Add(evt);

        await Task.CompletedTask;
    }
}
