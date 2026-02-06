using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record CompleteTripCommand(TripId TripId, UserId DriverId) : IRequest<ErrorOr<CompleteTripResult>>;

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
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
    }
}

public class CompleteTripCommandHandler(AppDbContext db, ITripFareService tripFareService) : IRequestHandler<CompleteTripCommand, ErrorOr<CompleteTripResult>>
{
    public async Task<ErrorOr<CompleteTripResult>> Handle(CompleteTripCommand request, CancellationToken cancellationToken)
    {
        // Find the trip
        var trip = await db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverId == request.DriverId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        var tripRoute = await db.TripRoutes
            .Include(tr => tr.TripGpsPoints)
            .FirstOrDefaultAsync(tr => tr.TripId == request.TripId, cancellationToken);

        if (tripRoute is null)
        {
            return TripErrors.Trip.NotFound;
        }

        tripRoute.ConstructPath();

        // Complete the trip
        var completeTripResult = trip.CompleteTrip();
        if (completeTripResult.IsError)
        {
            return completeTripResult.Errors;
        }

        // Update driver availability back to available
        var userId = request.DriverId;
        var driverStatus = await db.DriverStatuses
            .FirstAsync(ds => ds.UserId == userId, cancellationToken);

        var completeTripAvailabilityResult = driverStatus.CompleteTrip();
        if (completeTripAvailabilityResult.IsError)
        {
            return completeTripAvailabilityResult.Errors;
        }

        // Calculate actual trip fare using the service
        var tripFare = await tripFareService.CalculateActualFareAsync(trip, tripRoute, cancellationToken);
        var setFareResult = trip.SetTripFare(tripFare);
        if (setFareResult.IsError)
        {
            return setFareResult.Errors;
        }

        var payment = TripPayment.Cash(tripFare.TotalFare);
        var setPaymentResult = trip.SetTripPayment(payment);
        if (setPaymentResult.IsError)
        {
            return setPaymentResult.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CompleteTripResult(
            trip.Id,
            "Trip completed successfully",
            trip.CompletedAt!.Value,
            trip.TotalDuration,
            trip.TripFare?.TotalFare ?? trip.FinalFare.Amount);
    }
}
