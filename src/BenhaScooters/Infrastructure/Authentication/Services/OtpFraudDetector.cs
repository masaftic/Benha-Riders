using System.Text.Json;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface IOtpFraudDetector
{
    /// <summary>
    /// Analyze the current request for fraud signals. Returns a fraud score (0-100).
    /// Score >= 80 means block, 50-79 means flag for review.
    /// </summary>
    Task<OtpFraudResult> AnalyzeAsync(
        UserId? userId, string phoneNumber, string? ipAddress, CancellationToken ct);
}

public record OtpFraudResult(int Score, List<string> Signals)
{
    public bool IsBlocked => Score >= 80;
    public bool IsSuspicious => Score >= 50;
}

public class OtpFraudDetector : IOtpFraudDetector
{
    private readonly IMemoryCache _cache;
    private readonly OtpSecurityOptions _options;
    private readonly ILogger<OtpFraudDetector> _logger;

    // Cache key prefixes for fraud tracking
    private const string IpPhonesPrefix = "otp:fraud:ip-phones:";
    private const string UserIpsPrefix = "otp:fraud:user-ips:";

    public OtpFraudDetector(
        IMemoryCache cache,
        IOptions<OtpSecurityOptions> options,
        ILogger<OtpFraudDetector> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public Task<OtpFraudResult> AnalyzeAsync(
        UserId? userId, string phoneNumber, string? ipAddress, CancellationToken ct)
    {
        var score = 0;
        var signals = new List<string>();

        // Signal 1: Single IP requesting codes for many different phone numbers
        // (Indicates automated enumeration / SMS pumping attack)
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ipPhonesKey = IpPhonesPrefix + ipAddress;
            var phones = _cache.GetOrCreate(ipPhonesKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return new HashSet<string>();
            })!;

            phones.Add(phoneNumber);
            _cache.Set(ipPhonesKey, phones, TimeSpan.FromHours(1));

            if (phones.Count > _options.MaxDistinctPhonesPerIpPerHour)
            {
                var points = Math.Min(50, (phones.Count - _options.MaxDistinctPhonesPerIpPerHour) * 15);
                score += points;
                signals.Add($"IP_MULTIPLE_PHONES:{phones.Count} distinct phones from IP in last hour");
            }
        }

        // Signal 2: Single user using many different IPs
        // (Indicates credential sharing or distributed attack)
        if (userId.HasValue && !string.IsNullOrEmpty(ipAddress))
        {
            var userIpsKey = UserIpsPrefix + userId.Value;
            var ips = _cache.GetOrCreate(userIpsKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                return new HashSet<string>();
            })!;

            ips.Add(ipAddress);
            _cache.Set(userIpsKey, ips, TimeSpan.FromDays(1));

            if (ips.Count > _options.MaxDistinctIpsPerUserPerDay)
            {
                var points = Math.Min(40, (ips.Count - _options.MaxDistinctIpsPerUserPerDay) * 10);
                score += points;
                signals.Add($"USER_MULTIPLE_IPS:{ips.Count} distinct IPs for user in last day");
            }
        }

        // Signal 3: Suspicious IP patterns (private ranges via proxy, etc.)
        if (!string.IsNullOrEmpty(ipAddress))
        {
            if (ipAddress.StartsWith("10.") || ipAddress.StartsWith("172.") || ipAddress.StartsWith("192.168."))
            {
                // Internal/proxy IPs are less trustworthy for tracking — mild signal
                score += 5;
                signals.Add("INTERNAL_IP:Request from internal/proxy IP");
            }
        }

        // Signal 4: No IP available (unusual — headers stripped or misconfigured)
        if (string.IsNullOrEmpty(ipAddress))
        {
            score += 10;
            signals.Add("NO_IP:Client IP could not be determined");
        }

        if (score >= 50)
        {
            _logger.LogWarning(
                "OTP fraud analysis: Score={Score}, UserId={UserId}, Phone={Phone}, IP={Ip}, Signals=[{Signals}]",
                score, userId, phoneNumber, ipAddress, string.Join("; ", signals));
        }

        return Task.FromResult(new OtpFraudResult(score, signals));
    }

    /// <summary>
    /// Record a fraud-related observation for an IP address.
    /// This should be called when suspicious activity is confirmed.
    /// </summary>
    public void RecordSuspiciousIp(string ipAddress, string reason)
    {
        _logger.LogWarning("Suspicious IP recorded: {Ip} — {Reason}", ipAddress, reason);
    }
}
