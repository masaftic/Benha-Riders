using BenhaScooters.Domain.Users;

namespace BenhaScooters.Infrastructure.Notifications;

public interface ISignalRConnectionTracker
{
    Task RecordHeartbeat(UserId userId);
    Task<bool> IsConnectedAsync(UserId userId);
}
