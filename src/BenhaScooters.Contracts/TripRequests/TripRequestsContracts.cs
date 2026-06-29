using FluentValidation;

namespace BenhaScooters.Contracts.TripRequests;

public record RequestTripRequest(
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? DropoffAddress = null);

public record CancelTripRequestRequest(
    int TripRequestId,
    string? CancellationReason = null);

public class RequestTripRequestValidator : AbstractValidator<RequestTripRequest>
{
    public RequestTripRequestValidator()
    {
        RuleFor(x => x.PickupLatitude).InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees.");
        RuleFor(x => x.DropoffLatitude).InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees.");
        RuleFor(x => x.PickupLongitude).InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees.");
        RuleFor(x => x.DropoffLongitude).InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees.");
        RuleFor(x => x.DropoffAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.DropoffAddress))
            .WithMessage("Dropoff address must not exceed 500 characters");
    }
}
