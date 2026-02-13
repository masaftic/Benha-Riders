using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.TripRequests.Queries;

public record GetCurrentTripRequestQuery(UserId RiderId) : IRequest<ErrorOr<GetCurrentTripRequestResult>>;

public record GetCurrentTripRequestResult(
    int TripRequestId,
    TripRequestStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    DateTime RequestedAt,
    DateTime ExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? MatchedAt,
    string? MatchedDriverName);

public class GetCurrentTripRequestQueryHandler : IRequestHandler<GetCurrentTripRequestQuery, ErrorOr<GetCurrentTripRequestResult>>
{
    private readonly AppDbContext _db;

    public GetCurrentTripRequestQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetCurrentTripRequestResult>> Handle(GetCurrentTripRequestQuery request, CancellationToken cancellationToken)
    {
        // Active trip request statuses: NotConfirmed, Pending
        var activeStatuses = new[] { TripRequestStatus.Pending };

        var tripRequestResult = await _db.TripRequests
            .AsNoTracking()
            .Where(tr => tr.RiderId == request.RiderId 
                      && activeStatuses.Contains(tr.Status)
                      && tr.ExpiresAt > DateTime.UtcNow)
            .Select(tr => new
            {
                tr.Id,
                tr.Status,
                tr.PickupAddress,
                tr.DropoffAddress,
                tr.FinalFare,
                tr.RequestedAt,
                tr.ExpiresAt,
                tr.ConfirmedAt,
                tr.MatchedAt,
                MatchedDriverName = tr.MatchedDriverProfile != null 
                    ? tr.MatchedDriverProfile.PersonalInfo!.FullName 
                    : null
            })
            .OrderByDescending(tr => tr.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (tripRequestResult is null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        return new GetCurrentTripRequestResult(
            tripRequestResult.Id,
            tripRequestResult.Status,
            tripRequestResult.PickupAddress ?? "Unknown pickup location",
            tripRequestResult.DropoffAddress ?? "Unknown dropoff location",
            tripRequestResult.FinalFare.Amount,
            tripRequestResult.FinalFare.Distance,
            tripRequestResult.FinalFare.Time,
            tripRequestResult.RequestedAt,
            tripRequestResult.ExpiresAt,
            tripRequestResult.ConfirmedAt,
            tripRequestResult.MatchedAt,
            tripRequestResult.MatchedDriverName);
    }
}
