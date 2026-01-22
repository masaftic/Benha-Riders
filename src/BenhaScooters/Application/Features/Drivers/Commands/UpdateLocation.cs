using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Drivers.Commands;

public record UpdateLocationCommand(
    UserId DriverId,
    double Latitude,
    double Longitude) : IRequest<ErrorOr<UpdateLocationResponse>>;


public record UpdateLocationResponse(
    double Latitude,
    double Longitude);


public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}


public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, ErrorOr<UpdateLocationResponse>>
{
    private readonly AppDbContext _db;

    public UpdateLocationCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<UpdateLocationResponse>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var userId = request.DriverId;
        var driverLocation = new DriverLocation(userId, location);

        _db.DriverLocations.Update(driverLocation);

        var tripId = await _db.DriverStatuses
            .Where(ds => ds.UserId == userId && ds.Status == DriverAvailabilityStatus.OnTrip)
            .Select(ds => ds.CurrentTripId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tripId.HasValue)
        {
            var gpsPoint = new TripGpsPoint(
                tripId.Value,
                location,
                DateTime.UtcNow);

            _db.TripGpsPoints.Add(gpsPoint);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateLocationResponse(request.Latitude, request.Longitude);
    }
}
