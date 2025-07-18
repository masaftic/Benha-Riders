using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record CancelTripCommand(
    RiderId RiderId,
    TripRequestId TripRequestId,
    string? CancellationReason = null) : IRequest<ErrorOr<CancelTripResult>>;

public record CancelTripResult(
    TripRequestId TripRequestId,
    string Message,
    DateTime CancelledAt);

public class CancelTripCommandValidator : AbstractValidator<CancelTripCommand>
{
    public CancelTripCommandValidator()
    {
        RuleFor(x => x.RiderId)
            .NotEmpty()
            .WithMessage("Rider ID is required");

        RuleFor(x => x.TripRequestId)
            .NotEmpty()
            .WithMessage("Trip request ID is required");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.CancellationReason))
            .WithMessage("Cancellation reason must not exceed 500 characters");
    }
}

public class CancelTripCommandHandler(AppDbContext db) : IRequestHandler<CancelTripCommand, ErrorOr<CancelTripResult>>
{
    public async Task<ErrorOr<CancelTripResult>> Handle(CancelTripCommand request, CancellationToken cancellationToken)
    {
        // Find the trip request
        var tripRequest = await db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId && tr.RiderId == request.RiderId, cancellationToken);

        if (tripRequest == null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        // Check if trip can be cancelled
        if (tripRequest.Status == TripRequestStatus.Cancelled)
        {
            return TripErrors.TripRequest.AlreadyCancelled;
        }

        if (tripRequest.Status == TripRequestStatus.Matched)
        {
            return TripErrors.TripRequest.AlreadyMatched;
        }

        // Cancel the trip request
        tripRequest.Cancel(request.CancellationReason ?? "Cancelled by rider");
        
        await db.SaveChangesAsync(cancellationToken);

        return new CancelTripResult(
            tripRequest.Id,
            "Trip request cancelled successfully",
            DateTime.UtcNow);
    }
}
