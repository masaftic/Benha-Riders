using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.PendingActions.Queries;

public record GetPendingActionsQuery(UserId UserId) : IRequest<ErrorOr<GetPendingActionsResult>>;

public record GetPendingActionsResult(IReadOnlyList<PendingAction> Actions);

public record PendingAction(
    string Type,
    string Title,
    string Description,
    string Method,
    string Path,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int Priority,
    PendingTripRatingAction? TripRating);

public record PendingTripRatingAction(
    TripId TripId,
    UserId DriverId,
    string DriverName,
    string? DriverPhotoUrl,
    string PickupAddress,
    string DropoffAddress,
    Coordinate PickupLocation,
    Coordinate DropoffLocation,
    decimal FinalFare,
    DateTime CompletedAt);


public class GetPendingActionsQueryHandler(AppDbContext db, IS3Service s3Service)
    : IRequestHandler<GetPendingActionsQuery, ErrorOr<GetPendingActionsResult>>
{
    private const int PendingRatingLookbackDays = 3;

    public async Task<ErrorOr<GetPendingActionsResult>> Handle(
        GetPendingActionsQuery request,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-PendingRatingLookbackDays);

        var pendingRating = await db.Trips
            .AsNoTracking()
            .Where(t => t.RiderId == request.UserId
                        && t.Status == TripStatus.Completed
                        && t.CompletedAt.HasValue
                        && t.CompletedAt.Value >= cutoff
                        && !db.TripRatings.Any(r => r.TripId == t.Id && r.DriverRating.HasValue)
                        && !db.DriverRatingDismissals.Any(d => d.TripId == t.Id && d.RiderId == request.UserId))
            .OrderByDescending(t => t.CompletedAt)
            .Select(t => new PendingRatingProjection(
                t.Id,
                t.DriverId,
                t.DriverProfile.PersonalInfo!.FullName,
                t.DriverProfile.Documents
                    .Where(d => d.Type == DocumentType.DriverPhoto)
                    .Select(d => d.ImageUrl)
                    .FirstOrDefault(),
                t.PickupAddress,
                t.DropoffAddress,
                t.PickupLocation,
                t.DropoffLocation,
                t.FinalFare.Amount,
                t.CompletedAt!.Value))
            .FirstOrDefaultAsync(cancellationToken);

        List<PendingAction> actions = [];

        if (pendingRating is not null)
        {
            var driverPhotoUrl = pendingRating.DriverPhotoUrl is not null
                ? await s3Service.GetPreSignedUrlAsync(pendingRating.DriverPhotoUrl, TimeSpan.FromHours(1), cancellationToken)
                : null;

            actions.Add(new PendingAction(
                Type: "trip.driver_rating",
                Title: "Rate your driver",
                Description: $"How was your trip with {pendingRating.DriverName}?",
                Method: "POST",
                Path: $"/api/trips/{pendingRating.TripId}/rate",
                CreatedAt: pendingRating.CompletedAt,
                ExpiresAt: null,
                Priority: 100,
                TripRating: new PendingTripRatingAction(
                    pendingRating.TripId,
                    pendingRating.DriverId,
                    pendingRating.DriverName,
                    driverPhotoUrl,
                    pendingRating.PickupAddress ?? "Unknown pickup location",
                    pendingRating.DropoffAddress ?? "Unknown dropoff location",
                    Coordinate.FromPoint(pendingRating.PickupLocation),
                    Coordinate.FromPoint(pendingRating.DropoffLocation),
                    pendingRating.FinalFare,
                    pendingRating.CompletedAt)));
        }

        return new GetPendingActionsResult(actions);
    }

    private sealed record PendingRatingProjection(
        TripId TripId,
        UserId DriverId,
        string DriverName,
        string? DriverPhotoUrl,
        string? PickupAddress,
        string? DropoffAddress,
        NetTopologySuite.Geometries.Point PickupLocation,
        NetTopologySuite.Geometries.Point DropoffLocation,
        decimal FinalFare,
        DateTime CompletedAt);
}
