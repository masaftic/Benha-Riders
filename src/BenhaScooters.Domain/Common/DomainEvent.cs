namespace BenhaScooters.Domain.Common;

/// <summary>
/// Base class for domain events with common properties
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
