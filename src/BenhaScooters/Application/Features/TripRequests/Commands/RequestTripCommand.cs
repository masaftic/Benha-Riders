using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.TripRequests.Commands;

public record RequestTripCommand(
    UserId RiderId,
    Domain.Common.Geo.Coordinate PickupCoordinate,
    Domain.Common.Geo.Coordinate DropoffCoordinate,
    string? DropoffAddress = null) : IRequest<ErrorOr<RequestTripResult>>;

public record RequestTripResult(
    TripRequestId TripRequestId,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    DateTime RequestedAt);

public class RequestTripCommandValidator : AbstractValidator<RequestTripCommand>
{
    public RequestTripCommandValidator()
    {
        RuleFor(x => x.RiderId)
            .NotEmpty()
            .WithMessage("Rider ID is required");

        RuleFor(x => x.DropoffAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.DropoffAddress))
            .WithMessage("Dropoff address must not exceed 500 characters");
    }
}

public class RequestTripCommandHandler(
    AppDbContext db,
    IFareEstimator fareEstimator,
    IServiceAreaValidator serviceAreaValidator,
    IGoogleMapsService googleMapsService) : IRequestHandler<RequestTripCommand, ErrorOr<RequestTripResult>>
{
    public async Task<ErrorOr<RequestTripResult>> Handle(RequestTripCommand request, CancellationToken cancellationToken)
    {
        // Check if rider has active trip request
        var hasActiveRequest = await db.TripRequests
            .AnyAsync(tr => tr.RiderId == request.RiderId &&
                           tr.Status == TripRequestStatus.Pending && tr.ExpiresAt > DateTime.UtcNow,
                      cancellationToken);

        if (hasActiveRequest)
        {
            return AppErrors.Rider.HasActiveTripRequest();
        }

        // check if rider has active trip
        var hasActiveTrip = await db.Trips
            .AnyAsync(t => t.RiderId == request.RiderId &&
                           (t.Status == TripStatus.Assigned || t.Status == TripStatus.DriverArrived || t.Status == TripStatus.InProgress), cancellationToken);

        if (hasActiveTrip)
        {
            return AppErrors.Rider.HasActiveTrip();
        }

        
        if (!await serviceAreaValidator.AreLocationsWithinServiceAreaAsync(
            request.PickupCoordinate, 
            request.DropoffCoordinate, 
            cancellationToken: cancellationToken))
        {
            return AppErrors.ServiceArea.LocationNotCovered();
        }


        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(GeoConstants.SRID_WGS84);

        // Create location points
        var pickupCoordinate = request.PickupCoordinate;
        var dropoffCoordinate = request.DropoffCoordinate;

        // Reverse geocode pickup address
        var pickupGeocodeResult = await googleMapsService.ReverseGeocodeAsync(
            pickupCoordinate.Latitude,
            pickupCoordinate.Longitude,
            cancellationToken: cancellationToken);

        string? pickupAddress = null;
        if (!pickupGeocodeResult.IsError)
        {
            pickupAddress = pickupGeocodeResult.Value.FormattedAddress;
        }

        // Estimate fare
        var fareEstimate = await fareEstimator.EstimateFareAsync(pickupCoordinate.ToPoint(geometryFactory), dropoffCoordinate.ToPoint(geometryFactory), cancellationToken);

        // Create trip request
        var tripRequest = new TripRequest(
            request.RiderId,
            pickupCoordinate.ToPoint(geometryFactory),
            dropoffCoordinate.ToPoint(geometryFactory),
            pickupAddress,
            request.DropoffAddress,
            fareEstimate);

        await db.TripRequests.AddAsync(tripRequest, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new RequestTripResult(
            tripRequest.Id,
            fareEstimate.Amount,
            fareEstimate.Distance,
            fareEstimate.Time,
            tripRequest.RequestedAt);
    }
}
