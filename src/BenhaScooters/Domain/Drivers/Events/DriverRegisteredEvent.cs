using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers.Events;


public record DriverRegisteredEvent(
    DriverId DriverId,
    UserId UserId) : DomainEvent;
