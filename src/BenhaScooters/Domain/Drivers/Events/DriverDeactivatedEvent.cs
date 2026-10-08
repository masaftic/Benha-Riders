using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverDeactivatedEvent(
    UserId DriverId,
    string Reason) : DomainEvent;