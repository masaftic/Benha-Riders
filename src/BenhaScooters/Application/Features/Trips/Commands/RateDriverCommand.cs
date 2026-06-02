using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
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
            return AppErrors.Trip.NotFound();

        if (trip.RiderId != request.RiderId)
            return AppErrors.Rating.NotYourTrip();

        if (trip.Status != TripStatus.Completed)
            return AppErrors.Rating.TripNotCompleted();

        var tripRating = await _dbContext.TripRatings
            .FirstOrDefaultAsync(r => r.TripId == request.TripId, cancellationToken);

        if (tripRating is null)
        {
            tripRating = new TripRating(trip.Id, trip.DriverId, trip.RiderId);
            _dbContext.TripRatings.Add(tripRating);
        }

        var ratingResult = tripRating.SetDriverRating(request.Rating, request.Comment);
        if (ratingResult.IsError)
            return ratingResult.Errors;

        // Update driver stats with new rating
        var driverStats = await _dbContext.DriverStats
            .FirstOrDefaultAsync(ds => ds.UserId == trip.DriverId, cancellationToken);

        driverStats?.AddRating(request.Rating);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RateDriverResult(tripRating.Id, driverStats?.AverageRating ?? request.Rating);
    }
}
