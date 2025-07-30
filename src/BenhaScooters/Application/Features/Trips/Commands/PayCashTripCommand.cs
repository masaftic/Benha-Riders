using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.ValueObjects;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record PayCashForTripCommand(TripId TripId, decimal PaidAmount) : IRequest<ErrorOr<PayCashForTripResult>>;

public record PayCashForTripResult(TripId TripId, string Message, DateTime PaidAt, decimal FinalFare);


public class PayCashForTripCommandHandler : IRequestHandler<PayCashForTripCommand, ErrorOr<PayCashForTripResult>>
{
    private readonly AppDbContext _db;

    public PayCashForTripCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<PayCashForTripResult>> Handle(PayCashForTripCommand request, CancellationToken cancellationToken)
    {
        // Find the trip
        var trip = await _db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        if (trip.TripPayment == null)
        {
            return TripErrors.Trip.PaymentNotSet;
        }

        var result = trip.TripPayment.MarkAsPaid(request.PaidAmount);
        if (result.IsError) return result.Errors;

        await _db.SaveChangesAsync(cancellationToken);

        return new PayCashForTripResult(
            trip.Id,
            "Payment successful",
            DateTime.UtcNow,
            request.PaidAmount);
    }
}
