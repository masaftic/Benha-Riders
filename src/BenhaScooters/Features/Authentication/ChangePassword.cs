using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Features.Authentication.Services;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public class ChangePasswordRequestValidator : Validator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.NewPassword).WithMessage("Password confirmation must match new password.");
    }
}

public record ChangePasswordResponse(string Message);

public class ChangePasswordEndpoint(AppDbContext db, IPasswordHasher passwordHasher) : Endpoint<ChangePasswordRequest, ChangePasswordResponse>
{
    public override void Configure()
    {
        Post("/auth/change-password");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Change user password")
            .Produces<ChangePasswordResponse>()
            .Produces(400)
            .Produces(401));

        Summary(s =>
        {
            s.Summary = "Change user password";
            s.Description = "Allows users to change their password. Requires current password for verification.";
            s.ExampleRequest = new ChangePasswordRequest("currentPassword123", "newPassword456", "newPassword456");
        });
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            ThrowError("User not found.", errorCode: "UserNotFound", statusCode: 404);
            return;
        }

        if (!passwordHasher.Verify(user.PasswordHash, req.CurrentPassword))
        {
            ThrowError(p => p.CurrentPassword, "Current password is incorrect.", errorCode: "IncorrectCurrentPassword", statusCode: 400);
            return;
        }

        user.ChangePassword(passwordHasher.Hash(req.NewPassword));

        await db.SaveChangesAsync(ct);

        await SendAsync(new ChangePasswordResponse("Password changed successfully"), cancellation: ct);
    }
}
