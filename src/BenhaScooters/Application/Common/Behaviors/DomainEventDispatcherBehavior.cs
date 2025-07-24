using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that publishes domain events after command execution
/// This ensures domain events are only published if the command succeeds
/// </summary>
public class DomainEventDispatcherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;

    public DomainEventDispatcherBehavior(AppDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Execute the command first
        var response = await next();

        // Only process domain events if the command was successful
        // For ErrorOr types, check if there are errors
        if (IsErrorResponse(response))
        {
            return response;
        }

        // Collect and publish domain events in a separate transaction
        await PublishDomainEventsAsync(cancellationToken);

        return response;
    }

    private static bool IsErrorResponse<T>(T response)
    {
        // Check if response is an ErrorOr type with errors
        if (response != null && response.GetType().IsGenericType)
        {
            var genericType = response.GetType().GetGenericTypeDefinition();
            if (genericType.Name.StartsWith("ErrorOr"))
            {
                var isErrorProperty = response.GetType().GetProperty("IsError");
                if (isErrorProperty != null)
                {
                    return (bool)isErrorProperty.GetValue(response)!;
                }
            }
        }
        return false;
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEvents = new List<IDomainEvent>();

        // Collect domain events from all aggregate roots
        foreach (var entry in _dbContext.ChangeTracker.Entries<AggregateRoot>())
        {
            if (entry.Entity.DomainEvents.Any())
            {
                domainEvents.AddRange(entry.Entity.DomainEvents);
                entry.Entity.ClearDomainEvents();
            }
        }

        // Publish events sequentially to maintain order
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
