using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverActivatedEvent(
    DriverId DriverId) : DomainEvent;
