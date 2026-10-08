using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverOnboardingCompletedEvent(
    UserId DriverId,
    DateTime CompletedAt) : DomainEvent;
