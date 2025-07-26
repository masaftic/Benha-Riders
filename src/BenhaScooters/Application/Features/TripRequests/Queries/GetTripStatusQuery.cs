using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.TripRequests.Queries;

public record GetTripStatusQuery(
    RiderId RiderId,
    TripRequestId TripRequestId) : IRequest<ErrorOr<GetTripStatusResult>>;

public record GetTripStatusResult(
    TripRequestId TripRequestId,
    TripRequestStatus Status,
    string? MatchedDriverName,
    DateTime RequestedAt,
    DateTime? MatchedAt,
    DateTime? ExpiresAt,
    string? CancellationReason);

public class GetTripStatusQueryValidator : AbstractValidator<GetTripStatusQuery>
{
    public GetTripStatusQueryValidator()
    {
        RuleFor(x => x.RiderId.Value)
            .NotEmpty()
            .WithMessage("Rider ID is required");

        RuleFor(x => x.TripRequestId.Value)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class GetTripStatusQueryHandler(AppDbContext db) : IRequestHandler<GetTripStatusQuery, ErrorOr<GetTripStatusResult>>
{
    public async Task<ErrorOr<GetTripStatusResult>> Handle(GetTripStatusQuery request, CancellationToken cancellationToken)
    {
        // Find the trip request
        var tripStatusInfo = await db.TripRequests
            .Include(tr => tr.MatchedDriver)
            .AsNoTracking()
            .Where(tr => tr.RiderId == request.RiderId && tr.Id == request.TripRequestId)
            .Select(tr => new
            {
                tr.Id,
                tr.Status,
                MatchedDriverName = tr.MatchedDriver != null ? tr.MatchedDriver.PersonalInfo!.FullName : null,
                tr.RequestedAt,
                tr.MatchedAt,
                tr.ExpiresAt,
                tr.CancellationReason
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tripStatusInfo == null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        return new GetTripStatusResult(
            tripStatusInfo.Id,
            tripStatusInfo.Status,
            tripStatusInfo.MatchedDriverName,
            tripStatusInfo.RequestedAt,
            tripStatusInfo.MatchedAt,
            tripStatusInfo.ExpiresAt,
            tripStatusInfo.CancellationReason);
    }
}
