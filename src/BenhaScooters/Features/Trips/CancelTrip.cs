using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Trips;

public record CancelTripRequest(
    TripRequestId TripRequestId,
    string? CancellationReason = null
);

public class CancelTripRequestValidator : Validator<CancelTripRequest>
{
    public CancelTripRequestValidator()
    {
        RuleFor(x => x.TripRequestId)
            .NotEmpty()
            .WithMessage("Trip request ID is required");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.CancellationReason))
            .WithMessage("Cancellation reason must not exceed 500 characters");
    }
}

public record CancelTripResponse(
    TripRequestId TripRequestId,
    string Message,
    DateTime CancelledAt
);

public class CancelTripEndpoint(AppDbContext db) : Endpoint<CancelTripRequest, CancelTripResponse>
{
    public override void Configure()
    {
        Post("/trips/cancel");
        Claims(JwtClaims.Sub);
        Roles("Rider");
        Description(x => x
            .WithSummary("Cancel a trip request")
            .WithTags("Trips")
            .Produces<CancelTripResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Cancel a trip request";
            s.Description = "Allows riders to cancel their pending trip requests. Only pending trips can be cancelled.";
            s.ExampleRequest = new CancelTripRequest(TripRequestId.From(123), "Plans changed");
        });
    }

    public override async Task HandleAsync(CancelTripRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();
        
        // Get rider profile
        var riderId = await db.Riders
            .Where(r => r.UserId == userId)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (!riderId.IsInitialized())
        {
            ThrowError("Rider profile not found", 404);
        }

        // Find the trip request
        var tripRequest = await db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == req.TripRequestId && tr.RiderId == riderId, ct);

        if (tripRequest == null)
        {
            ThrowError("Trip request not found", 404);
        }

        // Check if trip can be cancelled
        if (tripRequest.Status == TripRequestStatus.Cancelled)
        {
            ThrowError("Trip request is already cancelled", 400);
        }

        if (tripRequest.Status == TripRequestStatus.Matched)
        {
            ThrowError("Trip has already started and cannot be cancelled here", 400);
        }

        // Cancel the trip request
        tripRequest.Cancel(req.CancellationReason ?? "Cancelled by rider");
        
        await db.SaveChangesAsync(ct);

        await SendAsync(new CancelTripResponse(
            tripRequest.Id,
            "Trip request cancelled successfully",
            DateTime.UtcNow
        ), cancellation: ct);
    }
}
