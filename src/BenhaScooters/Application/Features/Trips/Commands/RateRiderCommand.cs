using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record RateRiderCommand(TripId TripId, UserId DriverId, int Rating, string? Comment) : IRequest<ErrorOr<RateRiderResult>>;

public record RateRiderResult(int RatingId, decimal NewAverageRating);

public class RateRiderCommandHandler(AppDbContext dbContext) : IRequestHandler<RateRiderCommand, ErrorOr<RateRiderResult>>
{
    public async Task<ErrorOr<RateRiderResult>> Handle(RateRiderCommand request, CancellationToken cancellationToken)
    {
        var trip = await dbContext.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip == null)
            return AppErrors.Trip.NotFound();

        if (trip.DriverId != request.DriverId)
            return AppErrors.Rating.NotYourTrip();

        if (trip.Status != TripStatus.Completed)
            return AppErrors.Rating.TripNotCompleted();

        var tripRating = await dbContext.TripRatings
            .FirstOrDefaultAsync(r => r.TripId == request.TripId, cancellationToken);

        if (tripRating is null)
        {
            tripRating = new TripRating(trip.Id, trip.DriverId, trip.RiderId);
            dbContext.TripRatings.Add(tripRating);
        }

        var ratingResult = tripRating.SetRiderRating(request.Rating, request.Comment);
        if (ratingResult.IsError)
            return ratingResult.Errors;

        var riderProfile = await dbContext.RiderProfiles
            .FirstOrDefaultAsync(r => r.UserId == trip.RiderId, cancellationToken);

        riderProfile?.AddRating(request.Rating);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RateRiderResult(tripRating.Id, riderProfile?.AverageRating ?? request.Rating);
    }
}
