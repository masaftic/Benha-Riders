using System.Text;
using System.Text.Json;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface IOtpRateLimiter
{
    /// <summary>
    /// Check if a new OTP request is allowed. Returns an error if rate-limited.
    /// </summary>
    Task<ErrorOr<OtpRateLimitResult>> CheckSendRateLimitAsync(
        UserId userId, string phoneNumber, string? ipAddress, CancellationToken ct);

    /// <summary>
    /// Check if a verification attempt is allowed (brute-force protection).
    /// </summary>
    Task<ErrorOr<Success>> CheckVerifyRateLimitAsync(
        UserId userId, string? ipAddress, CancellationToken ct);
}

public record OtpRateLimitResult(int CooldownSeconds);

public class OtpRateLimiter : IOtpRateLimiter
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly OtpSecurityOptions _options;
    private readonly ILogger<OtpRateLimiter> _logger;

    // Cache key prefixes
    private const string UserHourlyPrefix = "otp:send:user:hourly:";
    private const string UserDailyPrefix = "otp:send:user:daily:";
    private const string IpHourlyPrefix = "otp:send:ip:hourly:";
    private const string IpDailyPrefix = "otp:send:ip:daily:";
    private const string PhoneHourlyPrefix = "otp:send:phone:hourly:";
    private const string PhoneDailyPrefix = "otp:send:phone:daily:";
    private const string UserLastSendPrefix = "otp:send:user:last:";
    private const string UserVerifyFailPrefix = "otp:verify:fail:user:hourly:";
    private const string UserVerifyLockPrefix = "otp:verify:lock:user:";

    public OtpRateLimiter(
        AppDbContext db,
        IMemoryCache cache,
        IOptions<OtpSecurityOptions> options,
        ILogger<OtpRateLimiter> logger)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ErrorOr<OtpRateLimitResult>> CheckSendRateLimitAsync(
        UserId userId, string phoneNumber, string? ipAddress, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);
        var dayAgo = now.AddDays(-1);

        // 1. Check cooldown (minimum time between requests)
        var lastSendKey = UserLastSendPrefix + userId;
        if (_cache.TryGetValue<DateTime>(lastSendKey, out var lastSend))
        {
            var elapsed = (now - lastSend).TotalSeconds;
            var requiredCooldown = GetProgressiveCooldown(userId);
            if (elapsed < requiredCooldown)
            {
                var retryAfter = (int)Math.Ceiling(requiredCooldown - elapsed);
                _logger.LogWarning("OTP cooldown active for user {UserId}. Retry after {Seconds}s", userId, retryAfter);
                return AppErrors.User.OtpRateLimitExceeded(retryAfter);
            }
        }

        // 2. Check per-user hourly limit
        var userHourlyCount = GetOrInitCounter(UserHourlyPrefix + userId, TimeSpan.FromHours(1));
        if (userHourlyCount >= _options.MaxRequestsPerUserPerHour)
        {
            _logger.LogWarning("User {UserId} exceeded hourly OTP limit ({Count}/{Max})",
                userId, userHourlyCount, _options.MaxRequestsPerUserPerHour);
            return AppErrors.User.OtpRateLimitExceeded(GetSecondsUntilNextHour());
        }

        // 3. Check per-user daily limit
        var userDailyCount = GetOrInitCounter(UserDailyPrefix + userId, TimeSpan.FromDays(1));
        if (userDailyCount >= _options.MaxRequestsPerUserPerDay)
        {
            _logger.LogWarning("User {UserId} exceeded daily OTP limit ({Count}/{Max})",
                userId, userDailyCount, _options.MaxRequestsPerUserPerDay);
            return AppErrors.User.OtpDailyLimitExceeded();
        }

        // 4. Check per-phone hourly/daily limits
        var phoneHourlyCount = GetOrInitCounter(PhoneHourlyPrefix + phoneNumber, TimeSpan.FromHours(1));
        if (phoneHourlyCount >= _options.MaxRequestsPerPhonePerHour)
        {
            _logger.LogWarning("Phone {Phone} exceeded hourly OTP limit", phoneNumber);
            return AppErrors.User.OtpRateLimitExceeded(GetSecondsUntilNextHour());
        }

        var phoneDailyCount = GetOrInitCounter(PhoneDailyPrefix + phoneNumber, TimeSpan.FromDays(1));
        if (phoneDailyCount >= _options.MaxRequestsPerPhonePerDay)
        {
            _logger.LogWarning("Phone {Phone} exceeded daily OTP limit", phoneNumber);
            return AppErrors.User.OtpDailyLimitExceeded();
        }

        // 5. Check per-IP limits (if IP available)
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ipHourlyCount = GetOrInitCounter(IpHourlyPrefix + ipAddress, TimeSpan.FromHours(1));
            if (ipHourlyCount >= _options.MaxRequestsPerIpPerHour)
            {
                _logger.LogWarning("IP {Ip} exceeded hourly OTP limit ({Count}/{Max})",
                    ipAddress, ipHourlyCount, _options.MaxRequestsPerIpPerHour);
                return AppErrors.User.OtpRateLimitExceeded(GetSecondsUntilNextHour());
            }

            var ipDailyCount = GetOrInitCounter(IpDailyPrefix + ipAddress, TimeSpan.FromDays(1));
            if (ipDailyCount >= _options.MaxRequestsPerIpPerDay)
            {
                _logger.LogWarning("IP {Ip} exceeded daily OTP limit", ipAddress);
                return AppErrors.User.OtpDailyLimitExceeded();
            }
        }

        // 6. All checks passed — compute the cooldown for the response
        var nextCooldown = GetProgressiveCooldownForCount(userHourlyCount + 1);
        return new OtpRateLimitResult(nextCooldown);
    }

    public Task<ErrorOr<Success>> CheckVerifyRateLimitAsync(
        UserId userId, string? ipAddress, CancellationToken ct)
    {
        // Check if user is in verification lockout
        var lockKey = UserVerifyLockPrefix + userId;
        if (_cache.TryGetValue<DateTime>(lockKey, out var lockExpiry))
        {
            if (DateTime.UtcNow < lockExpiry)
            {
                var retryAfter = (int)Math.Ceiling((lockExpiry - DateTime.UtcNow).TotalSeconds);
                _logger.LogWarning("User {UserId} is in verification lockout. Retry after {Seconds}s",
                    userId, retryAfter);
                return Task.FromResult<ErrorOr<Success>>(AppErrors.User.OtpVerificationLocked(retryAfter));
            }
            else
            {
                _cache.Remove(lockKey);
            }
        }

        // Check hourly failed attempt count
        var failKey = UserVerifyFailPrefix + userId;
        var failCount = GetOrInitCounter(failKey, TimeSpan.FromHours(1));
        if (failCount >= _options.MaxFailedAttemptsPerUserPerHour)
        {
            // Lock the user out
            var lockoutExpiry = DateTime.UtcNow.AddSeconds(_options.VerificationLockoutSeconds);
            _cache.Set(lockKey, lockoutExpiry, TimeSpan.FromSeconds(_options.VerificationLockoutSeconds));
            _logger.LogWarning("User {UserId} locked out due to {Count} failed verification attempts",
                userId, failCount);
            return Task.FromResult<ErrorOr<Success>>(AppErrors.User.OtpVerificationLocked(_options.VerificationLockoutSeconds));
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    /// <summary>
    /// Record that a send was made (increment counters, update last-send timestamp).
    /// Call AFTER successfully sending the OTP.
    /// </summary>
    public void RecordSend(UserId userId, string phoneNumber, string? ipAddress)
    {
        var now = DateTime.UtcNow;

        _cache.Set(UserLastSendPrefix + userId, now, TimeSpan.FromHours(1));

        IncrementCounter(UserHourlyPrefix + userId, TimeSpan.FromHours(1));
        IncrementCounter(UserDailyPrefix + userId, TimeSpan.FromDays(1));
        IncrementCounter(PhoneHourlyPrefix + phoneNumber, TimeSpan.FromHours(1));
        IncrementCounter(PhoneDailyPrefix + phoneNumber, TimeSpan.FromDays(1));

        if (!string.IsNullOrEmpty(ipAddress))
        {
            IncrementCounter(IpHourlyPrefix + ipAddress, TimeSpan.FromHours(1));
            IncrementCounter(IpDailyPrefix + ipAddress, TimeSpan.FromDays(1));
        }
    }

    /// <summary>
    /// Record a failed verification attempt.
    /// </summary>
    public void RecordVerificationFailure(UserId userId)
    {
        IncrementCounter(UserVerifyFailPrefix + userId, TimeSpan.FromHours(1));
    }

    // ─── Private Helpers ──────────────────────────────────────────────

    private int GetOrInitCounter(string key, TimeSpan window)
    {
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return 0;
        });
    }

    private void IncrementCounter(string key, TimeSpan window)
    {
        var current = GetOrInitCounter(key, window);
        // Re-set with the same expiry window — this is approximate but safe for rate limiting
        _cache.Set(key, current + 1, window);
    }

    private int GetProgressiveCooldown(UserId userId)
    {
        var userHourlyCount = GetOrInitCounter(UserHourlyPrefix + userId, TimeSpan.FromHours(1));
        return GetProgressiveCooldownForCount(userHourlyCount);
    }

    private int GetProgressiveCooldownForCount(int requestCount)
    {
        var tiers = _options.ProgressiveCooldownSeconds;
        if (tiers.Length == 0) return _options.CooldownSeconds;

        var index = Math.Min(requestCount, tiers.Length) - 1;
        if (index < 0) return _options.CooldownSeconds;
        return tiers[index];
    }

    private static int GetSecondsUntilNextHour()
    {
        var now = DateTime.UtcNow;
        var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        return (int)Math.Ceiling((nextHour - now).TotalSeconds);
    }
}
