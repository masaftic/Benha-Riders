using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Drivers.Commands;

public record UpdateLocationCommand(
    DriverId DriverId,
    double Latitude,
    double Longitude,
    double Heading,
    double Speed) : IRequest<ErrorOr<UpdateLocationResponse>>;

public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Heading).InclusiveBetween(0, 360);
        RuleFor(x => x.Speed).GreaterThanOrEqualTo(0);
    }
}

public record UpdateLocationResponse(
    double Latitude,
    double Longitude,
    double Heading,
    double Speed);


public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, ErrorOr<UpdateLocationResponse>>
{
    private readonly AppDbContext _db;

    public UpdateLocationCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<UpdateLocationResponse>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var driverLocation = await _db.DriverLocations
            .FirstOrDefaultAsync(dl => dl.DriverId == request.DriverId, cancellationToken);

        if (driverLocation is null)
        {
            driverLocation = new DriverLocation(
                request.DriverId,
                new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                request.Heading,
                request.Speed,
                DateTime.UtcNow
            );

            _db.DriverLocations.Add(driverLocation);
        }
        else
        {
            driverLocation.UpdateLocation(
                new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                request.Heading,
                request.Speed
            );
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateLocationResponse(
                request.Latitude,
                request.Longitude,
                request.Heading,
                request.Speed);
    }
}
