using BenhaScooters.Data;
using BenhaScooters.Domain;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding.AdminActions;


public record ApproveDriverRequest(UserId UserId);

public class ApproveDriverRequestValidator : Validator<ApproveDriverRequest>
{
    public ApproveDriverRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Driver user ID is required.");
    }
}

public record ApproveDriverResponse(string Message);

public class ApproveDriverEndpoint : Endpoint<ApproveDriverRequest, ApproveDriverResponse>
{
    private readonly AppDbContext _db;

    public ApproveDriverEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/admin/driver/approve");
        Roles("Admin");
        Description(x => x
            .WithSummary("Approve driver onboarding application")
            .Produces<ApproveDriverResponse>()
            .Produces(400)
            .Produces(404));
    }

    public override async Task HandleAsync(ApproveDriverRequest req, CancellationToken ct)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == req.UserId, ct);

        if (driver == null)
        {
            ThrowError("Driver not found.", 
                errorCode: "DriverNotFound", statusCode: 404);
            return;
        }

        try
        {
            driver.CompleteOnboarding();
            await _db.SaveChangesAsync(ct);

            var response = new ApproveDriverResponse("Driver application approved successfully.");
            await SendOkAsync(response, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
    }
}