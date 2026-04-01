using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record CancelTripRequestCommand(
    TripRequestId TripRequestId,
    UserId RiderId,
    string? CancellationReason = null) : IRequest<ErrorOr<CancelTripRequestResult>>;

public record CancelTripRequestResult(
    TripRequestId TripRequestId,
    string Message,
    DateTime CancelledAt);

public class CancelTripCommandValidator : AbstractValidator<CancelTripRequestCommand>
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
            .WithMessage("سبب الإلغاء يجب ألا يتجاوز 500 حرف.");
    }
}

public class CancelTripRequestCommandHandler(AppDbContext db) : IRequestHandler<CancelTripRequestCommand, ErrorOr<CancelTripRequestResult>>
{
    public async Task<ErrorOr<CancelTripRequestResult>> Handle(CancelTripRequestCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Find the trip request
            var tripRequest = await db.TripRequests
                .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId && tr.RiderId == request.RiderId, cancellationToken);

            if (tripRequest == null)
            {
                return TripErrors.TripRequest.NotFound;
            }


            // Cancel the trip request
            var result = tripRequest.Cancel(request.CancellationReason ?? "Cancelled by rider");
            if (result.IsError) return result.Errors;

            // Also cancel any associated matching session
            var matchingSession = await db.MatchingSessions
                .Include(ms => ms.MatchAttempts)
                .FirstOrDefaultAsync(ms => ms.TripRequestId == request.TripRequestId, cancellationToken);

            if (matchingSession != null && matchingSession.IsActive)
            {
                matchingSession.Cancel(true, "Trip request cancelled by rider");
            }
            
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CancelTripRequestResult(
                tripRequest.Id,
                "Trip request cancelled successfully",
                DateTime.UtcNow);
        }
        catch (Exception)
        {
            // Transaction will be automatically rolled back
            throw;
        }
    }
}
