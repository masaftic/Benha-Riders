using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetCurrentTripQuery(UserId UserId) : IRequest<ErrorOr<GetCurrentTripResult>>;

public record GetCurrentTripResult(
    TripId TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    DateTime CreatedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    RiderInfo? Rider,
    DriverInfo? Driver);



public class GetCurrentTripQueryHandler : IRequestHandler<GetCurrentTripQuery, ErrorOr<GetCurrentTripResult>>
{
    private readonly AppDbContext _db;

    public GetCurrentTripQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetCurrentTripResult>> Handle(GetCurrentTripQuery request, CancellationToken cancellationToken)
    {
        // Active trip statuses: Assigned, DriverArrived, InProgress
        var activeStatuses = new[] { TripStatus.Assigned, TripStatus.DriverArrived, TripStatus.InProgress };

        var tripResult = await _db.Trips
            .AsNoTracking()
            .Where(t => (t.DriverId == request.UserId || t.RiderId == request.UserId) 
                     && activeStatuses.Contains(t.Status))
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.PickupAddress,
                t.DropoffAddress,
                t.FinalFare,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                RiderName = t.RiderProfile.PreferredName ?? t.RiderProfile.User.Name,
                RiderPhoneNumber = t.RiderProfile.User.PhoneNumber,
                DriverName = t.DriverProfile.PersonalInfo!.FullName,
                DriverPhoneNumber = t.DriverProfile.User.PhoneNumber,
                DriverVehicleColor = t.DriverProfile.Vehicle!.Color,
                DriverVehicleModel = t.DriverProfile.Vehicle!.Model,
                DriverVehicleLicensePlate = t.DriverProfile.Vehicle!.LicensePlate
            })
            .OrderByDescending(t => t.AssignedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (tripResult is null)
        {
            return TripErrors.Trip.NotFound;
        }

        return new GetCurrentTripResult(
            tripResult.Id,
            tripResult.Status,
            tripResult.PickupAddress ?? "Unknown pickup location",
            tripResult.DropoffAddress ?? "Unknown dropoff location",
            tripResult.FinalFare.Amount,
            tripResult.AssignedAt,
            tripResult.DriverArrivedAt,
            tripResult.StartedAt,
            new RiderInfo(
                tripResult.RiderName,
                tripResult.RiderPhoneNumber!),
            new DriverInfo(
                tripResult.DriverName,
                tripResult.DriverPhoneNumber!,
                tripResult.DriverVehicleModel,
                tripResult.DriverVehicleColor,
                tripResult.DriverVehicleLicensePlate));
    }
}
