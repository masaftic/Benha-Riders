using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record CompleteTripCommand(
    DriverId DriverId,
    TripId TripId,
    double FinalLatitude,
    double FinalLongitude) : IRequest<ErrorOr<CompleteTripResult>>;

public record CompleteTripResult(
    TripId TripId,
    string Message,
    DateTime CompletedAt,
    TimeSpan? TotalDuration,
    decimal FinalFare);

public class CompleteTripCommandValidator : AbstractValidator<CompleteTripCommand>
{
    public CompleteTripCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripId.Value)
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

public class CompleteTripCommandHandler(AppDbContext db, ITripFareService tripFareService) : IRequestHandler<CompleteTripCommand, ErrorOr<CompleteTripResult>>
{
    public async Task<ErrorOr<CompleteTripResult>> Handle(CompleteTripCommand request, CancellationToken cancellationToken)
    {
        // Find the trip
        var trip = await db.Trips
            .Include(t => t.TripRoute)
            .ThenInclude(tr => tr!.TripGpsPoints)
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverId == request.DriverId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        // Create final location point
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var finalLocation = geometryFactory.CreatePoint(new Coordinate(request.FinalLongitude, request.FinalLatitude));

        // Add final GPS point
        var finalGpsPoint = new TripGpsPoint(trip.TripRoute!.Id, request.DriverId, finalLocation, 0, 0, DateTime.UtcNow);
        trip.TripRoute.AddPoint(finalGpsPoint);

        // Complete the trip
        var completeTripResult = trip.CompleteTrip();
        if (completeTripResult.IsError)
        {
            return completeTripResult.Errors;
        }
        
        // Update driver availability back to available
        var driverAvailability = await db.DriverAvailabilities
            .FirstAsync(da => da.DriverId == request.DriverId, cancellationToken);

        var completeTripAvailabilityResult = driverAvailability.CompleteTrip();
        if (completeTripAvailabilityResult.IsError)
        {
            return completeTripAvailabilityResult.Errors;
        }

        // Calculate actual trip fare using the service
        var tripFare = await tripFareService.CalculateActualFareAsync(trip, cancellationToken);
        var setFareResult = trip.SetTripFare(tripFare);
        if (setFareResult.IsError)
        {
            return setFareResult.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        var totalDuration = trip.StartedAt.HasValue && trip.CompletedAt.HasValue
            ? (TimeSpan?)(trip.CompletedAt.Value - trip.StartedAt.Value)
            : null;

        return new CompleteTripResult(
            trip.Id,
            "Trip completed successfully",
            trip.CompletedAt!.Value,
            totalDuration,
            trip.TripFare?.TotalFare ?? trip.EstimatedFare.Amount);
    }
}
