using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Queries;

public record GetDriverAvailabilityQuery(UserId DriverId) : IRequest<ErrorOr<GetDriverAvailabilityResponse>>;

public record GetDriverAvailabilityResponse(
    DriverAvailabilityStatus Status,
    DateTime LastStatusChange,
    TimeSpan? OnlineSessionDuration,
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
        var userId = request.DriverId;
        
        // Get driver status
        var driverStatus = await _db.DriverStatuses
            .FirstOrDefaultAsync(ds => ds.UserId == userId, cancellationToken);

        if (driverStatus == null)
        {
            // Create default status if none exists
            driverStatus = new DriverStatus(userId);
            await _db.DriverStatuses.AddAsync(driverStatus, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Calculate current session duration
        TimeSpan? sessionDuration = null;
        if (driverStatus.OnlineSessionStart.HasValue && driverStatus.Status == DriverAvailabilityStatus.Online)
        {
            sessionDuration = DateTime.UtcNow - driverStatus.OnlineSessionStart.Value;
        }

        return new GetDriverAvailabilityResponse(
            driverStatus.Status,
            driverStatus.LastStatusChange,
            sessionDuration,
            driverStatus.CurrentTripId?.Value);
    }
}
