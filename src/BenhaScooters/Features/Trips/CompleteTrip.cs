using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

public record CompleteTripRequest(TripId TripId, double FinalLatitude, double FinalLongitude);

public class CompleteTripRequestValidator : Validator<CompleteTripRequest>
{
    public CompleteTripRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
            
        RuleFor(x => x.FinalLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Final latitude must be between -90 and 90");
            
        RuleFor(x => x.FinalLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Final longitude must be between -180 and 180");
    }
}

public record CompleteTripResponse(
    TripId TripId,
    string Message,
    DateTime CompletedAt,
    TimeSpan? TotalDuration,
    decimal FinalFare
);

public class CompleteTripEndpoint(AppDbContext db) : Endpoint<CompleteTripRequest, CompleteTripResponse>
{
    public override void Configure()
    {
        Post("/trips/{tripId}/complete");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        Description(x => x
            .WithSummary("Complete the trip")
            .WithTags("Trips")
            .Produces<CompleteTripResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Complete the trip";
            s.Description = "Allows drivers to complete the trip when they reach the destination. Updates driver availability back to available.";
            s.ExampleRequest = new CompleteTripRequest(TripId.From(123), 30.0444, 31.2357);
        });
    }

    public override async Task HandleAsync(CompleteTripRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        // Get driver profile
        var driverId = await db.Drivers
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(ct);

        if (!driverId.IsInitialized())
        {
            ThrowError("Driver profile not found", 404);
        }

        // Find the trip
        var trip = await db.Trips
            .Include(t => t.TripRoute)
            .FirstOrDefaultAsync(t => t.Id == req.TripId && t.DriverId == driverId, ct);

        if (trip == null)
        {
            ThrowError("Trip not found or you are not the assigned driver", 404);
        }

        // Create final location point
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var finalLocation = geometryFactory.CreatePoint(new Coordinate(req.FinalLongitude, req.FinalLatitude));

        // Add final GPS point
        var finalGpsPoint = new TripGpsPoint(trip.Id, driverId, finalLocation, 0, 0, DateTime.UtcNow);
        db.TripGpsPoints.Add(finalGpsPoint);

        // Update the trip route with final location if route exists
        if (trip.TripRoute != null)
        {
            var existingCoordinates = trip.TripRoute.Path.Coordinates.ToList();
            existingCoordinates.Add(finalLocation.Coordinate);
            
            var finalLineString = geometryFactory.CreateLineString(existingCoordinates.ToArray());
            var totalDuration = trip.StartedAt.HasValue 
                ? DateTime.UtcNow - trip.StartedAt.Value 
                : TimeSpan.Zero;
                
            trip.TripRoute.SetPath(finalLineString, totalDuration);
        }

        // Complete the trip
        trip.CompleteTrip(finalLocation);

        // Calculate final fare
        var actualFare = CalculateFinalFare(trip);
        db.TripFares.Add(actualFare);

        // Update driver availability back to available
        var driverAvailability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(x => x.DriverId == driverId, ct);

        if (driverAvailability != null)
        {
            driverAvailability.CompleteTrip();
        }


        await db.SaveChangesAsync(ct);

        await SendAsync(new CompleteTripResponse(
            trip.Id,
            "Trip completed successfully",
            trip.CompletedAt!.Value,
            trip.TotalDuration,
            actualFare.TotalFare
        ), cancellation: ct);
    }

    private static TripFare CalculateFinalFare(Trip trip)
    {
        // Fare calculation parameters
        const decimal baseFare = 5.00m; // Base fare in EGP
        const decimal ratePerKm = 2.50m; // Rate per kilometer
        const decimal ratePerMinute = 0.50m; // Rate per minute
        const decimal surgeMultiplier = 1.0m; // No surge for now

        // Calculate distance from route if available, otherwise use estimate
        var actualDistance = 0.0;
        if (trip.TripRoute?.Path != null)
        {
            // Calculate actual distance from route (in kilometers)
            // NetTopologySuite uses meters, so convert to kilometers
            actualDistance = trip.TripRoute.Path.Length / 1000.0;
        }
        else
        {
            // Fallback to estimated distance
            actualDistance = trip.EstimatedFare.Distance;
        }

        // Calculate time in minutes
        var actualTime = trip.TotalDuration?.TotalMinutes ?? trip.EstimatedFare.Time;

        // Calculate fare components
        var distanceFare = (decimal)actualDistance * ratePerKm;
        var timeFare = (decimal)actualTime * ratePerMinute;

        return new TripFare(trip.Id, baseFare, distanceFare, timeFare, surgeMultiplier);
    }
}


