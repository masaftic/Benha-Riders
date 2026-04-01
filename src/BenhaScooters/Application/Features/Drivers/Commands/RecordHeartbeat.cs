using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Notifications;

public record RecordHeartbeatCommand(UserId DriverId) : IRequest<Unit>;

public class RecordHeartbeatCommandHandler : IRequestHandler<RecordHeartbeatCommand, Unit>
{
    private readonly ISignalRConnectionTracker _connectionTracker;

    public RecordHeartbeatCommandHandler(ISignalRConnectionTracker connectionTracker)
    {
        _connectionTracker = connectionTracker;
    }

    public async Task<Unit> Handle(RecordHeartbeatCommand request, CancellationToken cancellationToken)
    {
        await _connectionTracker.RecordHeartbeat(request.DriverId);
        return Unit.Value;
    }
}
