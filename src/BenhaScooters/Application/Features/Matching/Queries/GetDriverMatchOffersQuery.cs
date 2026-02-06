using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.Queries;

public record GetDriverMatchOffersQuery(UserId DriverId) : IRequest<ErrorOr<GetDriverMatchOffersResult>>;

public record GetDriverMatchOffersResult(
    List<DriverMatchOfferDto> MatchOffers);

public record DriverMatchOfferDto(
    DriverMatchAttemptId DriverMatchAttemptId,
    string RiderName,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress,
    string? DropoffAddress,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    double DistanceToPickup,
    double EstimatedArrivalTime,
    DateTime OfferedAt,
    DateTime ExpiresAt);

public class GetDriverMatchOffersQueryValidator : AbstractValidator<GetDriverMatchOffersQuery>
{
    public GetDriverMatchOffersQueryValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");
    }
}

public class GetDriverMatchOffersQueryHandler(AppDbContext db) 
    : IRequestHandler<GetDriverMatchOffersQuery, ErrorOr<GetDriverMatchOffersResult>>
{
    public async Task<ErrorOr<GetDriverMatchOffersResult>> Handle(
        GetDriverMatchOffersQuery request, 
        CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        // Check if driver exists and is available
        var driverStatus = await db.DriverStatuses
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverStatus == null)
        {
            return DriverErrors.DriverNotFound;
        }

        if (driverStatus.Status != DriverAvailabilityStatus.Online)
        {
            return TripErrors.Driver.NotOnline;
        }

        // Get pending match offers for this driver
        var matchOffers = await db.DriverMatchAttempts
            .AsNoTracking()
            .Where(ma => ma.DriverUserId == request.DriverId && 
                        ma.Status == MatchAttemptStatus.Pending && 
                        ma.ExpiresAt > DateTime.UtcNow)
            .OrderBy(ma => ma.CreatedAt)
            .Select(ma =>  new
            {
                ma.Id,
                RiderName = ma.MatchingSession.TripRequest.RiderProfile.PreferredName ?? ma.MatchingSession.TripRequest.RiderProfile.User.Name,
                ma.MatchingSession.TripRequest.PickupLocation,
                ma.MatchingSession.TripRequest.DropoffLocation,
                ma.MatchingSession.TripRequest.PickupAddress,
                ma.MatchingSession.TripRequest.DropoffAddress,
                ma.MatchingSession.TripRequest.FinalFare.Amount,
                ma.MatchingSession.TripRequest.FinalFare.Distance,
                ma.MatchingSession.TripRequest.FinalFare.Time,
                ma.DistanceToPickup,
                ma.EstimatedArrivalTime,
                ma.CreatedAt,
                ma.ExpiresAt
            })
            .ToListAsync(cancellationToken);


        return new GetDriverMatchOffersResult(matchOffers.Select(x => new DriverMatchOfferDto(
            x.Id,
            x.RiderName,
            x.PickupLocation.Y, // Latitude
            x.PickupLocation.X, // Longitude
            x.DropoffLocation.Y, // Latitude
            x.DropoffLocation.X, // Longitude
            x.PickupAddress,
            x.DropoffAddress,
            x.Amount,
            x.Distance,
            x.Time,
            x.DistanceToPickup,
            x.EstimatedArrivalTime,
            x.CreatedAt,
            x.ExpiresAt)).ToList());
    }
}
