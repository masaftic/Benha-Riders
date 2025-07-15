// using BenhaScooters.Data;
// using BenhaScooters.Domain.Trips.Enums;
// using BenhaScooters.Shared.Security;
// using FastEndpoints;
// using Microsoft.EntityFrameworkCore;
// using NetTopologySuite.Geometries;

// namespace BenhaScooters.Features.Trips;

// public record TripRequestDto(
//     int Id,
//     double PickupLatitude,
//     double PickupLongitude,
//     double DropoffLatitude,
//     double DropoffLongitude,
//     string? PickupAddress,
//     string? DropoffAddress,
//     decimal EstimatedFare,
//     double EstimatedDistance,
//     double EstimatedDuration,
//     TripRequestStatus Status,
//     DateTime RequestedAt,
//     DateTime? ExpiresAt
// );

// public record GetTripRequestsResponse(List<TripRequestDto> TripRequests);

// public class GetTripRequestsEndpoint(AppDbContext db) : EndpointWithoutRequest<GetTripRequestsResponse>
// {
//     public override void Configure()
//     {
//         Get("/trips/requests");
//         Claims(JwtClaims.Sub);
//         Roles("Rider");
//         Description(x => x
//             .WithSummary("Get rider's trip requests")
//             .WithTags("Trips")
//             .Produces<GetTripRequestsResponse>()
//             .Produces(401)
//             .Produces(404));
//     }

//     public override async Task HandleAsync(CancellationToken ct)
//     {
//         var userId = this.GetCurrentUserId();
        
//         // Get rider profile
//         var riderId = await db.Riders
//             .Where(r => r.UserId == userId)
//             .Select(r => r.Id)
//             .FirstOrDefaultAsync(ct);

//         if (!riderId.IsInitialized())
//         {
//             ThrowError("Rider profile not found", 404);
//         }

//         var tripRequests = await db.TripRequests
//             .Where(tr => tr.RiderId == riderId)
//             .OrderByDescending(tr => tr.RequestedAt)
//             .Take(20) // Limit to last 20 requests
//             .Select(tr => new TripRequestDto(
//                 tr.Id.Value,
//                 tr.PickupLocation.Y, // Latitude
//                 tr.PickupLocation.X, // Longitude
//                 tr.DropoffLocation.Y,
//                 tr.DropoffLocation.X,
//                 tr.PickupAddress,
//                 tr.DropoffAddress,
//                 tr.EstimatedFare.EstimatedAmount,
//                 tr.EstimatedFare.Distance,
//                 tr.EstimatedFare.EstimatedTime,
//                 tr.Status,
//                 tr.RequestedAt,
//                 tr.ExpiresAt
//             ))
//             .ToListAsync(ct);

//         await SendAsync(new GetTripRequestsResponse(tripRequests), cancellation: ct);
//     }
// }
