using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Ratings;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.PendingActions.Commands;

public record DismissDriverRatingActionCommand(TripId TripId, UserId RiderId)
    : IRequest<ErrorOr<DismissDriverRatingActionResult>>;

public record DismissDriverRatingActionResult(TripId TripId, bool Dismissed, DateTime? DismissedAt);

public class DismissDriverRatingActionCommandHandler(AppDbContext db)
    : IRequestHandler<DismissDriverRatingActionCommand, ErrorOr<DismissDriverRatingActionResult>>
{
    public async Task<ErrorOr<DismissDriverRatingActionResult>> Handle(
        DismissDriverRatingActionCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await db.Trips
            .AsNoTracking()
            .Where(t => t.Id == request.TripId)
            .Select(t => new
            {
                t.Id,
                t.RiderId,
                t.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (trip is null)
            return AppErrors.Trip.NotFound();

        if (trip.RiderId != request.RiderId)
            return AppErrors.Rating.NotYourTrip();

        if (trip.Status != TripStatus.Completed)
            return AppErrors.Rating.TripNotCompleted();

        var alreadyRated = await db.TripRatings
            .AnyAsync(r => r.TripId == request.TripId && r.DriverRating.HasValue, cancellationToken);

        if (alreadyRated)
            return new DismissDriverRatingActionResult(request.TripId, Dismissed: false, DismissedAt: null);

        var existingDismissal = await db.DriverRatingDismissals
            .AsNoTracking()
            .Where(d => d.TripId == request.TripId && d.RiderId == request.RiderId)
            .Select(d => d.DismissedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingDismissal != default)
            return new DismissDriverRatingActionResult(request.TripId, Dismissed: true, existingDismissal);

        var dismissal = new DriverRatingDismissal(request.TripId, request.RiderId);
        db.DriverRatingDismissals.Add(dismissal);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();

            var dismissedAt = await db.DriverRatingDismissals
                .AsNoTracking()
                .Where(d => d.TripId == request.TripId && d.RiderId == request.RiderId)
                .Select(d => d.DismissedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (dismissedAt == default)
                throw;

            return new DismissDriverRatingActionResult(request.TripId, Dismissed: true, dismissedAt);
        }

        return new DismissDriverRatingActionResult(request.TripId, Dismissed: true, dismissal.DismissedAt);
    }
}
