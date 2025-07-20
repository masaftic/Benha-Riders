using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Infrastructure.Trips.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record RequestTripCommand(
    RiderId RiderId,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress = null,
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
        RuleFor(x => x.RiderId.Value)
            .NotEmpty()
            .WithMessage("Rider ID is required");

        RuleFor(x => x.PickupLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Pickup latitude must be between -90 and 90");

        RuleFor(x => x.PickupLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Pickup longitude must be between -180 and 180");

        RuleFor(x => x.DropoffLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Dropoff latitude must be between -90 and 90");

        RuleFor(x => x.DropoffLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Dropoff longitude must be between -180 and 180");

        RuleFor(x => x.PickupAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.PickupAddress))
            .WithMessage("Pickup address must not exceed 500 characters");

        RuleFor(x => x.DropoffAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.DropoffAddress))
            .WithMessage("Dropoff address must not exceed 500 characters");
    }
}

public class RequestTripCommandHandler(
    AppDbContext db,
    IFareEstimator fareEstimator) : IRequestHandler<RequestTripCommand, ErrorOr<RequestTripResult>>
{
    public async Task<ErrorOr<RequestTripResult>> Handle(RequestTripCommand request, CancellationToken cancellationToken)
    {
        // Check if rider has active trip request
        var hasActiveRequest = await db.TripRequests
            .AnyAsync(tr => tr.RiderId == request.RiderId && 
                           tr.Status == Domain.Trips.Enums.TripRequestStatus.Pending && tr.ExpiresAt > DateTime.UtcNow,
                      cancellationToken);

        if (hasActiveRequest)
        {
            return TripErrors.Rider.HasActiveTripRequest;
        }

        // Create location points
        var pickupLocation = new Point(request.PickupLongitude, request.PickupLatitude) { SRID = 4326 };
        var dropoffLocation = new Point(request.DropoffLongitude, request.DropoffLatitude) { SRID = 4326 };

        // Estimate fare
        var fareEstimate = await fareEstimator.EstimateFareAsync(pickupLocation, dropoffLocation, cancellationToken);

        // Create trip request
        var tripRequest = new TripRequest(
            request.RiderId,
            pickupLocation,
            dropoffLocation,
            request.PickupAddress,
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
