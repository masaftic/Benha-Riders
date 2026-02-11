using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record CancelTripCommand(
    TripId TripId,
    UserId UserId,
    string CancellationReason) : IRequest<ErrorOr<CancelTripResult>>;

public record CancelTripResult(
    TripId TripId,
    string Message);

public class CancelTripCommandHandler : IRequestHandler<CancelTripCommand, ErrorOr<CancelTripResult>>
{
    private readonly AppDbContext _db;

    public CancelTripCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<CancelTripResult>> Handle(CancelTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        // Verify the user is either the driver or rider
        if (trip.DriverId != request.UserId && trip.RiderId != request.UserId)
        {
            return TripErrors.Trip.NotFound;
        }

        var cancelResult = trip.CancelTrip(request.UserId, request.CancellationReason);
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CancelTripResult(
            request.TripId,
            "تم إلغاء الرحلة بنجاح");
    }
}
