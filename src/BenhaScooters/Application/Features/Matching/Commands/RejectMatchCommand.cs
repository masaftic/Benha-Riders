using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.Commands;

public record RejectMatchCommand(
    UserId DriverId,
    DriverMatchAttemptId DriverMatchAttemptId,
    string? Reason = null) : IRequest<ErrorOr<Success>>;


public class RejectMatchCommandValidator : AbstractValidator<RejectMatchCommand>
{
    public RejectMatchCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.DriverMatchAttemptId)
            .NotEmpty()
            .WithMessage("Driver match attempt ID is required");
    }
}

public class RejectMatchCommandHandler(AppDbContext db) : IRequestHandler<RejectMatchCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(RejectMatchCommand request, CancellationToken cancellationToken)
    {
        // Find the matching session for this trip request
        var matchAttempt = await db.DriverMatchAttempts
            .Include(ma => ma.MatchingSession)
            .Where(ma => ma.Id == request.DriverMatchAttemptId
                        && ma.DriverUserId == request.DriverId
                        && ma.Status == MatchAttemptStatus.Pending)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (matchAttempt == null)
            return AppErrors.Matching.Attempt.NotFound();
        
        var matchingSession = matchAttempt.MatchingSession;

        if (matchingSession == null)
        {
            return AppErrors.Matching.Session.NotFound();
        }

        // Use the domain aggregate to handle the match rejection
        var rejectResult = matchingSession.RejectMatch(request.DriverId, request.Reason);
        if (rejectResult.IsError)
        {
            return rejectResult.Errors;
        }

        // Save changes to persist the domain state and publish events
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
