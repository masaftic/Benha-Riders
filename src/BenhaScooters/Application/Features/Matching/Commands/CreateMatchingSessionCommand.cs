using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.Commands;

public record CreateMatchingSessionCommand(
    TripRequestId TripRequestId) : IRequest<ErrorOr<CreateMatchingSessionResult>>;

public record CreateMatchingSessionResult(
    MatchingSessionId MatchingSessionId,
    DateTime CreatedAt);

public class CreateMatchingSessionCommandValidator : AbstractValidator<CreateMatchingSessionCommand>
{
    public CreateMatchingSessionCommandValidator()
    {
        RuleFor(x => x.TripRequestId.Value)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class CreateMatchingSessionCommandHandler(AppDbContext db) 
    : IRequestHandler<CreateMatchingSessionCommand, ErrorOr<CreateMatchingSessionResult>>
{
    public async Task<ErrorOr<CreateMatchingSessionResult>> Handle(CreateMatchingSessionCommand request, CancellationToken cancellationToken)
    {
        // Check if trip request exists and is still pending
        var tripRequest = await db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId, cancellationToken);

        if (tripRequest == null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        if (!tripRequest.CanBeAssigned)
        {
            return TripErrors.TripRequest.NotPending;
        }

        // Check if matching session already exists for this trip request
        var existingSession = await db.MatchingSessions
            .FirstOrDefaultAsync(ms => ms.TripRequestId == request.TripRequestId, cancellationToken);

        if (existingSession != null)
        {
            // Return existing session if it's still active
            if (existingSession.IsActive)
            {
                return new CreateMatchingSessionResult(
                    existingSession.Id,
                    existingSession.CreatedAt);
            }
            
            // Remove expired/completed session
            db.MatchingSessions.Remove(existingSession);
        }

        // Create new matching session
        var matchingSession = new MatchingSession(request.TripRequestId, 3, [1, 3, 5]); // TODO: get values from config or something

        db.MatchingSessions.Add(matchingSession);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateMatchingSessionResult(
            matchingSession.Id,
            matchingSession.CreatedAt);
    }
}
