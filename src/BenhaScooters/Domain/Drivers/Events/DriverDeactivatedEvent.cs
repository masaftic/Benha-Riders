using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverDeactivatedEvent(
    DriverId DriverId,
    string Reason) : DomainEvent;