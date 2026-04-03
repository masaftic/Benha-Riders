using BenhaScooters.Domain.Users;

namespace BenhaScooters.Infrastructure.Notifications;

public interface ISignalRConnectionTracker
{
    Task TrackConnectedAsync(UserId userId, string connectionId);
    Task TrackDisconnectedAsync(UserId userId, string connectionId);
    Task RecordHeartbeat(UserId userId);
    Task<bool> IsConnectedAsync(UserId userId);
    Task<bool> HasActiveConnectionAsync(UserId userId);
}
