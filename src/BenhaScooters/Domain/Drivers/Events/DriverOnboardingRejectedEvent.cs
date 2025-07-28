using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverOnboardingRejectedEvent(
    DriverId DriverId,
    string RejectionReason) : DomainEvent;
