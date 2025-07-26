using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.Commands;

public record RejectMatchCommand(
    DriverId DriverId,
    TripRequestId TripRequestId,
    string? Reason = null) : IRequest<ErrorOr<RejectMatchResult>>;

public record RejectMatchResult(
    MatchingSessionId SessionId,
    string Message,
    DateTime RejectedAt);

public class RejectMatchCommandValidator : AbstractValidator<RejectMatchCommand>
{
    public RejectMatchCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripRequestId.Value)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class RejectMatchCommandHandler(AppDbContext db) : IRequestHandler<RejectMatchCommand, ErrorOr<RejectMatchResult>>
{
    public async Task<ErrorOr<RejectMatchResult>> Handle(RejectMatchCommand request, CancellationToken cancellationToken)
    {
        // Find the matching session for this trip request
        var matchingSession = await db.MatchingSessions
            .Include(ms => ms.MatchAttempts)
            .FirstOrDefaultAsync(ms => ms.TripRequestId == request.TripRequestId, cancellationToken);

        if (matchingSession == null)
        {
            return MatchingErrors.Session.NotFound;
        }

        // Use the domain aggregate to handle the match rejection
        var rejectResult = matchingSession.RejectMatch(request.DriverId, request.Reason);
        if (rejectResult.IsError)
        {
            return rejectResult.Errors;
        }

        // Save changes to persist the domain state and publish events
        await db.SaveChangesAsync(cancellationToken);

        return new RejectMatchResult(
            matchingSession.Id,
            "Match rejected successfully",
            DateTime.UtcNow);
    }
}
