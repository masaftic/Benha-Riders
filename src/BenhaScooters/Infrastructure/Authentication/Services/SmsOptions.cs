using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.Authentication.Services;

/// <summary>
/// Configuration for SMS sending via WhySMS API.
/// </summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>
    /// WhySMS API base URL.
    /// </summary>
    [Required]
    public string ApiUrl { get; set; } = "https://bulk.whysms.com/api/v3/sms/send";

    /// <summary>
    /// WhySMS API key (from user secrets).
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// Sender ID displayed to recipients.
    /// </summary>
    [Required]
    public string SenderId { get; set; } = null!;

    /// <summary>
    /// Enable/disable actual SMS sending. If false, uses dummy service.
    /// </summary>
    public bool EnableRealSmsSending { get; set; } = true;

    /// <summary>
    /// Timeout for HTTP requests in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}
