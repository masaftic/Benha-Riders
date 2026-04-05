namespace BenhaScooters.Application.Workflows;

public interface IMessageDispatcher
{
    Task Dispatch<TMessage>(TMessage message, CancellationToken ct = default);
}


public class MediatorMessageDispatcher(IMediator mediator) : IMessageDispatcher
{
    public async Task Dispatch<TMessage>(TMessage message, CancellationToken ct = default)
    {
        switch (message)
        {
            case INotification notification:
                await mediator.Publish((object)notification, ct);
                break;
            case IRequest request:
                // Important: send as object so MediatR resolves the concrete runtime request type
                // (e.g. StartMatchingSession), not IRequest itself.
                await mediator.Send((object)request, ct);
                break;
            default:
                throw new InvalidOperationException($"Unsupported message type: {typeof(TMessage).Name}");
        }
    }
}
