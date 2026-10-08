using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverOnboardingStepCompletedEvent(
    UserId DriverId
    ) : DomainEvent;
