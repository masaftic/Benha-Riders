using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Queries;

public record GetDriverAvailabilityQuery(DriverId DriverId) : IRequest<ErrorOr<GetDriverAvailabilityResponse>>;

public record GetDriverAvailabilityResponse(
    DriverStatus Status,
    DateTime LastStatusChange,
    TimeSpan? OnlineSessionDuration,
    TimeSpan TotalOnlineTime,
    int? CurrentTripId);

public class GetDriverAvailabilityQueryHandler : IRequestHandler<GetDriverAvailabilityQuery, ErrorOr<GetDriverAvailabilityResponse>>
{
    private readonly AppDbContext _db;

    public GetDriverAvailabilityQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetDriverAvailabilityResponse>> Handle(GetDriverAvailabilityQuery request, CancellationToken cancellationToken)
    {
        // Get driver availability
        var availability = await _db.DriverAvailabilities
            .FirstOrDefaultAsync(da => da.DriverId == request.DriverId, cancellationToken);

        if (availability == null)
        {
            // Create default availability if none exists
            availability = new DriverAvailability(request.DriverId);
            await _db.DriverAvailabilities.AddAsync(availability, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Calculate current session duration
        TimeSpan? sessionDuration = null;
        if (availability.OnlineSessionStart.HasValue && availability.Status == DriverStatus.Online)
        {
            sessionDuration = DateTime.UtcNow - availability.OnlineSessionStart.Value;
        }

        return new GetDriverAvailabilityResponse(
            availability.Status,
            availability.LastStatusChange,
            sessionDuration,
            availability.TotalOnlineTime,
            availability.CurrentTripId?.Value);
    }
}
