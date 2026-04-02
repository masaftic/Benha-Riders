using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Application.Features.Matching.Settings;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        RuleFor(x => x.TripRequestId)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class CreateMatchingSessionCommandHandler(AppDbContext db, IOptions<MatchingSessionOptions> options) 
    : IRequestHandler<CreateMatchingSessionCommand, ErrorOr<CreateMatchingSessionResult>>
{
    public async Task<ErrorOr<CreateMatchingSessionResult>> Handle(CreateMatchingSessionCommand request, CancellationToken cancellationToken)
    {
        // Check if trip request exists and is still pending
        var tripRequest = await db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId, cancellationToken);

        if (tripRequest == null)
        {
            return AppErrors.TripRequest.NotFound();
        }

        if (!tripRequest.CanBeAssigned)
        {
            return AppErrors.TripRequest.NotPending();
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

        // Create new matching session using configured rounds and offers
        var matchingSessionResult = MatchingSession.Create(
            request.TripRequestId,
            numberOfRounds: options.Value.NumberOfRounds,
            offersPerRound: options.Value.OffersPerRound.ToList());
        
        if (matchingSessionResult.IsError)
            return matchingSessionResult.Errors;

        var matchingSession = matchingSessionResult.Value;

        db.MatchingSessions.Add(matchingSession);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateMatchingSessionResult(
            matchingSession.Id,
            matchingSession.CreatedAt);
    }
}
