using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Ratings;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record RateDriverCommand(TripId TripId, UserId RiderId, int Rating, string? Comment) : IRequest<ErrorOr<RateDriverResult>>;

public record RateDriverResult(int RatingId, decimal NewAverageRating);

public class RateDriverCommandHandler : IRequestHandler<RateDriverCommand, ErrorOr<RateDriverResult>>
{
    private readonly AppDbContext _dbContext;

    public RateDriverCommandHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<RateDriverResult>> Handle(RateDriverCommand request, CancellationToken cancellationToken)
    {
        var trip = await _dbContext.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip == null)
            return TripErrors.Trip.NotFound;

        if (trip.RiderId != request.RiderId)
            return RatingErrors.NotYourTrip;

        if (trip.Status != TripStatus.Completed)
            return RatingErrors.TripNotCompleted;

        // Check if already rated
        var alreadyRated = await _dbContext.DriverRatings
            .AnyAsync(r => r.TripId == request.TripId && r.RiderId == request.RiderId, cancellationToken);

        if (alreadyRated)
            return RatingErrors.AlreadyRated;

        var ratingResult = DriverRating.Create(trip.Id, request.RiderId, trip.DriverId, request.Rating, request.Comment);
        if (ratingResult.IsError)
            return ratingResult.Errors;

        _dbContext.DriverRatings.Add(ratingResult.Value);

        // Update driver stats with new rating
        var driverStats = await _dbContext.DriverStats
            .FirstOrDefaultAsync(ds => ds.UserId == trip.DriverId, cancellationToken);

        driverStats?.AddRating(request.Rating);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RateDriverResult(ratingResult.Value.Id, driverStats?.AverageRating ?? request.Rating);
    }
}
