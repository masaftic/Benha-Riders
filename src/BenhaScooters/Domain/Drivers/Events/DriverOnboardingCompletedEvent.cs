using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverOnboardingCompletedEvent(
    DriverId DriverId,
    DateTime CompletedAt) : DomainEvent;
