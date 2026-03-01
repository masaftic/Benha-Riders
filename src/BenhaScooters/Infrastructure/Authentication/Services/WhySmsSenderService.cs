using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.Authentication.Services;

/// <summary>
/// Production SMS service using WhySMS API.
/// </summary>
public class WhySmsSenderService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly AppDbContext _db;
    private readonly ILogger<WhySmsSenderService> _logger;

    public WhySmsSenderService(
        HttpClient httpClient,
        IOptions<SmsOptions> options,
        AppDbContext db,
        ILogger<WhySmsSenderService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _db = db;
        _logger = logger;
    }

    public async Task SendSmsAsync(UserId userId, PhoneNumber phoneNumber, string message)
    {
        await SendSmsInternalAsync(userId, phoneNumber, SmsType.General, message, null);
    }

    public async Task SendVerificationCodeAsync(UserId userId, PhoneNumber phoneNumber, string code)
    {
        var message = $"رمز التحقق الخاص بك هو: {code}. صالح لمدة 5 دقائق.";
        await SendSmsInternalAsync(userId, phoneNumber, SmsType.Verification, message, null);
    }

    public async Task SendPasswordResetCodeAsync(UserId userId, PhoneNumber phoneNumber, string code)
    {
        var message = $"رمز إعادة تعيين كلمة المرور: {code}. صالح لمدة 5 دقائق.";
        await SendSmsInternalAsync(userId, phoneNumber, SmsType.Verification, message, null);
    }

    private async Task SendSmsInternalAsync(
        UserId userId,
        PhoneNumber phoneNumber,
        SmsType type,
        string message,
        string? ipAddress)
    {
        var phoneStr = phoneNumber.ToString();

        // Create SMS log entry
        var smsLog = new SmsLog(userId, phoneStr, type, message, ipAddress);
        _db.SmsLogs.Add(smsLog);
        await _db.SaveChangesAsync();

        try
        {
            // Build request
            var request = new WhySmsRequest
            {
                Recipient = phoneStr,
                SenderId = _options.SenderId,
                Type = type == SmsType.Verification ? "OTP" : "plain",
                Message = message
            };

            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending SMS to {Phone} via WhySMS API", phoneStr);

            // Send HTTP request
            var response = await _httpClient.PostAsync("", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("WhySMS response: {StatusCode} - {Body}", response.StatusCode, responseBody);

            // Deserialize response
            var smsResponse = JsonSerializer.Deserialize<WhySmsResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (smsResponse?.Status == "success" && smsResponse.Data != null)
            {
                // Update log with success
                smsLog.MarkDelivered(
                    smsResponse.Data.Id.ToString(),
                    smsResponse.Data.Uid,
                    smsResponse.Data.Cost,
                    smsResponse.Data.SmsCount);

                _logger.LogInformation(
                    "SMS delivered successfully to {Phone}. Provider ID: {ProviderId}, Cost: {Cost}",
                    phoneStr, smsResponse.Data.Id, smsResponse.Data.Cost);
            }
            else
            {
                // Mark as failed
                var errorMsg = smsResponse?.Message ?? "Unknown error";
                smsLog.MarkFailed(errorMsg);

                _logger.LogError("SMS sending failed for {Phone}: {Error}", phoneStr, errorMsg);
            }

            await _db.SaveChangesAsync();
        }
        catch (HttpRequestException ex)
        {
            smsLog.MarkFailed($"HTTP error: {ex.Message}");
            await _db.SaveChangesAsync();

            _logger.LogError(ex, "HTTP error while sending SMS to {Phone}", phoneStr);
            throw new InvalidOperationException($"Failed to send SMS: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            smsLog.MarkFailed("Request timeout");
            await _db.SaveChangesAsync();

            _logger.LogError(ex, "Timeout while sending SMS to {Phone}", phoneStr);
            throw new InvalidOperationException("SMS sending timeout", ex);
        }
        catch (Exception ex)
        {
            smsLog.MarkFailed($"Unexpected error: {ex.Message}");
            await _db.SaveChangesAsync();

            _logger.LogError(ex, "Unexpected error while sending SMS to {Phone}", phoneStr);
            throw;
        }
    }
}

// ──── DTOs for WhySMS API ────────────────────────────────────────────

internal class WhySmsRequest
{
    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = null!;

    [JsonPropertyName("sender_id")]
    public string SenderId { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
}

internal class WhySmsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("data")]
    public WhySmsData? Data { get; set; }
}

internal class WhySmsData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("uid")]
    public string Uid { get; set; } = null!;

    [JsonPropertyName("to")]
    public string To { get; set; } = null!;

    [JsonPropertyName("from")]
    public string From { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("sms_count")]
    public int SmsCount { get; set; }

    [JsonPropertyName("media_url")]
    public string? MediaUrl { get; set; }
}
