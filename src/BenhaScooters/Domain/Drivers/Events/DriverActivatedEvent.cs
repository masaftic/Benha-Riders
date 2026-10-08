using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverActivatedEvent(
    UserId DriverId) : DomainEvent;
