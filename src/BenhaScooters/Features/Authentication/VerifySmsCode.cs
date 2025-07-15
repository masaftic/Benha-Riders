using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Shared.Validation;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record VerifySmsCodeRequest(string PhoneNumber, string Code);

public class VerifySmsCodeRequestValidator : Validator<VerifySmsCodeRequest>
{
    public VerifySmsCodeRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("Invalid phone number format.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");
    }
}

public record VerifySmsCodeResponse(string Message, bool IsVerified);

public class VerifySmsCodeEndpoint : Endpoint<VerifySmsCodeRequest, VerifySmsCodeResponse>
{
    private readonly AppDbContext _db;

    public VerifySmsCodeEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/auth/verify-sms-code");
        AllowAnonymous();
        Description(x => x
            .WithSummary("Verify SMS verification code")
            .Produces<VerifySmsCodeResponse>()
            .Produces(400)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Verify SMS verification code";
            s.Description = "Verifies the 6-digit SMS code sent to the user's phone number. Marks the phone number as verified upon successful verification.";
            s.ExampleRequest = new VerifySmsCodeRequest("+1234567890", "123456");
        });
    }

    public override async Task HandleAsync(VerifySmsCodeRequest req, CancellationToken ct)
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
            await SendAsync(new VerifySmsCodeResponse("Phone number is already verified.", true), cancellation: ct);
            return;
        }

        // Find the verification code
        var verificationCode = await _db.SmsVerificationCodes
            .Where(x => x.UserId == user.Id && x.PhoneNumber == normalizedPhone && x.Code == req.Code)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (verificationCode is null)
        {
            ThrowError("Invalid verification code.", errorCode: "InvalidCode", statusCode: 400);
            return;
        }

        if (!verificationCode.IsValid)
        {
            string errorMessage = verificationCode.IsUsed ? "Verification code has already been used." : "Verification code has expired.";
            ThrowError(errorMessage, errorCode: "InvalidCode", statusCode: 400);
            return;
        }

        // Mark code as used and verify user's phone number
        verificationCode.MarkAsUsed();
        user.VerifyPhoneNumber();

        await _db.SaveChangesAsync(ct);

        await SendAsync(new VerifySmsCodeResponse("Phone number verified successfully.", true), cancellation: ct);
    }
}
