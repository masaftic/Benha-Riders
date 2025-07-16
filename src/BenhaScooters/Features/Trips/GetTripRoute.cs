using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Trips;

public record GetTripRouteRequest(TripId TripId);

public class GetTripRouteRequestValidator : Validator<GetTripRouteRequest>
{
    public GetTripRouteRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
    }
}

public record GetTripRouteResponse(
    TripId TripId,
    List<GpsPointDto> GpsPoints,
    string? RouteGeometry,
    TimeSpan? Duration,
    int TotalPoints
);

public record GpsPointDto(
    double Latitude,
    double Longitude,
    double Heading,
    double Speed,
    DateTime Timestamp
);

public class GetTripRouteEndpoint(AppDbContext db) : Endpoint<GetTripRouteRequest, GetTripRouteResponse>
{
    public override void Configure()
    {
        Get("/trips/{tripId}/route");
        Claims(JwtClaims.Sub);
        Roles("Driver", "Rider");
        Description(x => x
            .WithSummary("Get trip route and GPS points")
            .WithTags("Trips")
            .Produces<GetTripRouteResponse>()
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Get trip route";
            s.Description = "Retrieves the complete route and GPS points for a trip. Available to both drivers and riders.";
            s.ExampleRequest = new GetTripRouteRequest(TripId.From(123));
        });
    }

    public override async Task HandleAsync(GetTripRouteRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();
        
        // Check if user is either the driver or rider of this trip
        var trip = await db.Trips
            .Include(t => t.Driver)
            .Include(t => t.Rider)
            .Include(t => t.TripRoute)
            .FirstOrDefaultAsync(t => t.Id == req.TripId, ct);

        if (trip == null)
        {
            ThrowError("Trip not found", 404);
        }

        // Verify user has access to this trip
        var hasAccess = trip.Driver.UserId == userId || trip.Rider.UserId == userId;
        if (!hasAccess)
        {
            ThrowError("You don't have access to this trip", 403);
        }

        // Get GPS points
        var gpsPoints = await db.TripGpsPoints
            .Where(gp => gp.TripId == req.TripId)
            .OrderBy(gp => gp.Timestamp)
            .Select(gp => new GpsPointDto(
                gp.Latitude,
                gp.Longitude,
                gp.Heading,
                gp.Speed,
                gp.Timestamp
            ))
            .ToListAsync(ct);

        await SendAsync(new GetTripRouteResponse(
            trip.Id,
            gpsPoints,
            trip.TripRoute?.Path?.AsText(), // WKT format of the route
            trip.TripRoute?.Duration,
            gpsPoints.Count
        ), cancellation: ct);
    }
}
