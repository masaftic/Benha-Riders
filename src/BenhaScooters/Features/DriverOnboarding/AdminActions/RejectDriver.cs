using BenhaScooters.Data;
using BenhaScooters.Domain;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding.AdminActions;


public record RejectDriverRequest(UserId DriverUserId, string Reason);

public class RejectDriverRequestValidator : Validator<RejectDriverRequest>
{
    public RejectDriverRequestValidator()
    {
        RuleFor(x => x.DriverUserId)
            .NotEmpty().WithMessage("Driver user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters.");
    }
}

public record RejectDriverResponse(string Message);

public class RejectDriverEndpoint : Endpoint<RejectDriverRequest, RejectDriverResponse>
{
    private readonly AppDbContext _db;

    public RejectDriverEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/admin/driver/reject");
        Roles("Admin");
        Description(x => x
            .WithSummary("Reject driver onboarding application")
            .Produces<RejectDriverResponse>()
            .Produces(400)
            .Produces(404));
    }

    public override async Task HandleAsync(RejectDriverRequest req, CancellationToken ct)
    {
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == req.DriverUserId, ct);

        if (driverProfile == null)
        {
            ThrowError("Driver profile not found.", 
                errorCode: "DriverProfileNotFound", statusCode: 404);
            return;
        }

        try
        {
            driverProfile.RejectOnboarding(req.Reason);
            await _db.SaveChangesAsync(ct);

            var response = new RejectDriverResponse("Driver application rejected successfully.");
            await SendOkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
    }
}
