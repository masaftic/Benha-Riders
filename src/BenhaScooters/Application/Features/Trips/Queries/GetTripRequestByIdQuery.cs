using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetTripRequestByIdQuery(
    DriverId DriverId,
    TripRequestId TripRequestId) : IRequest<ErrorOr<GetTripRequestByIdResult>>;

public record GetTripRequestByIdResult(
    int TripRequestId,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress,
    string? DropoffAddress,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    DateTime RequestedAt,
    DateTime ExpiresAt,
    TripRequestStatus Status);

public class GetTripRequestByIdQueryHandler(AppDbContext db) : IRequestHandler<GetTripRequestByIdQuery, ErrorOr<GetTripRequestByIdResult>>
{
    public async Task<ErrorOr<GetTripRequestByIdResult>> Handle(GetTripRequestByIdQuery request, CancellationToken cancellationToken)
    {
        // Find the trip request
        var tripRequest = await db.TripRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId, cancellationToken);

        if (tripRequest == null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        return new GetTripRequestByIdResult(
            tripRequest.Id.Value,
            tripRequest.PickupLocation.Y, // Latitude
            tripRequest.PickupLocation.X, // Longitude
            tripRequest.DropoffLocation.Y, // Latitude
            tripRequest.DropoffLocation.X, // Longitude
            tripRequest.PickupAddress,
            tripRequest.DropoffAddress,
            tripRequest.EstimatedFare.Amount,
            tripRequest.EstimatedFare.Distance,
            tripRequest.EstimatedFare.Time,
            tripRequest.RequestedAt,
            tripRequest.ExpiresAt,
            tripRequest.Status);
    }
}
