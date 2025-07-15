using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Trips;

public record GetTripStatusRequest(TripRequestId TripRequestId);

public class GetTripStatusRequestValidator : Validator<GetTripStatusRequest>
{
    public GetTripStatusRequestValidator()
    {
        RuleFor(x => x.TripRequestId)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public record GetTripStatusResponse(
    TripRequestId TripRequestId,
    TripRequestStatus Status,
    string? AssignedDriverName,
    DateTime RequestedAt,
    DateTime? AcceptedAt,
    DateTime? ExpiresAt,
    string? CancellationReason
);

public class GetTripStatusEndpoint(AppDbContext db) : Endpoint<GetTripStatusRequest, GetTripStatusResponse>
{
    public override void Configure()
    {
        Get("/trips/{TripRequestId}/status");
        Claims(JwtClaims.Sub);
        Roles("Rider");
        Description(x => x
            .WithSummary("Get trip request status")
            .WithTags("Trips")
            .Produces<GetTripStatusResponse>()
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Get trip request status";
            s.Description = "Retrieves the current status of a trip request including driver details if assigned. Only accessible by the rider who created the request.";
            s.ExampleRequest = new GetTripStatusRequest(TripRequestId.From(123));
        });
    }

    public override async Task HandleAsync(GetTripStatusRequest req, CancellationToken ct)
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
            .Include(tr => tr.AssignedDriver)
            .FirstOrDefaultAsync(tr => tr.Id == req.TripRequestId && tr.RiderId == riderId, ct);

        if (tripRequest == null)
        {
            ThrowError("Trip request not found", 404);
        }

        var driverName = tripRequest.AssignedDriver?.PersonalInfo?.FullName;

        await SendAsync(new GetTripStatusResponse(
            tripRequest.Id,
            tripRequest.Status,
            driverName,
            tripRequest.RequestedAt,
            tripRequest.AssignedAt,
            tripRequest.ExpiresAt,
            tripRequest.CancellationReason
        ), cancellation: ct);
    }
}
