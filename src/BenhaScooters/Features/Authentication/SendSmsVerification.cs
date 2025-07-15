using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Features.Authentication.Services;
using BenhaScooters.Shared.Validation;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record SendSmsVerificationRequest(string PhoneNumber);

public class SendSmsVerificationRequestValidator : Validator<SendSmsVerificationRequest>
{
    public SendSmsVerificationRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("Invalid phone number format.");
    }
}

public record SendSmsVerificationResponse(string Message);

public class SendSmsVerificationEndpoint : Endpoint<SendSmsVerificationRequest, SendSmsVerificationResponse>
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;

    public SendSmsVerificationEndpoint(AppDbContext db, ISmsService smsService)
    {
        _db = db;
        _smsService = smsService;
    }

    public override void Configure()
    {
        Post("/auth/send-sms-verification");
        AllowAnonymous();
        Description(x => x
            .WithSummary("Send SMS verification code")
            .Produces<SendSmsVerificationResponse>()
            .Produces(400)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Send SMS verification code";
            s.Description = "Sends a 6-digit verification code to the specified phone number. Code expires after 10 minutes.";
            s.ExampleRequest = new SendSmsVerificationRequest("+1234567890");
        });
    }

    public override async Task HandleAsync(SendSmsVerificationRequest req, CancellationToken ct)
    {
        var normalizedPhone = Domain.User.NormalizePhone(PhoneNumber.From(req.PhoneNumber));
        
        // Find user by phone number
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumberNormalized == normalizedPhone, ct);

        if (user is null)
        {
            ThrowError("User with this phone number not found.", errorCode: "UserNotFound", statusCode: 404);
            return;
        }

        if (user.PhoneNumberVerified)
        {
            ThrowError("Phone number is already verified.", errorCode: "PhoneAlreadyVerified", statusCode: 400);
            return;
        }

        // Check if there's a recent verification code (prevent spam)
        var recentCode = await _db.SmsVerificationCodes
            .Where(x => x.UserId == user.Id && x.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recentCode != null)
        {
            ThrowError("Please wait before requesting another verification code.", errorCode: "TooManyRequests", statusCode: 400);
            return;
        }

        // Generate and save verification code
        var code = SmsVerificationCode.GenerateCode();
        var verificationCode = new SmsVerificationCode(user.Id, normalizedPhone, code, TimeSpan.FromMinutes(10));
        
        _db.SmsVerificationCodes.Add(verificationCode);
        await _db.SaveChangesAsync(ct);

        // Send SMS
        await _smsService.SendVerificationCodeAsync(normalizedPhone, code);

        await SendAsync(new SendSmsVerificationResponse("Verification code sent successfully."), cancellation: ct);
    }
}
