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


public class CompleteTripCommandHandler(AppDbContext db) : IRequestHandler<CompleteTripCommand, ErrorOr<CompleteTripResult>>
{
    public async Task<ErrorOr<CompleteTripResult>> Handle(CompleteTripCommand request, CancellationToken cancellationToken)
    {
        // Find the trip
        var trip = await db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverId == request.DriverId, cancellationToken);

        if (trip == null)
        {
            return AppErrors.Trip.NotFound();
        }
        
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

        var payment = TripPayment.Cash(trip.FinalFare.Amount);
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
            trip.FinalFare.Amount);
    }
}
