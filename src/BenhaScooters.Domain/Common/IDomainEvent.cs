using MediatR;

namespace BenhaScooters.Domain.Common;

/// <summary>
/// Marker interface for domain events that can be published and handled
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// When this event occurred
    /// </summary>
    DateTime OccurredAt { get; }
}
