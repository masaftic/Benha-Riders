using System.Collections.Concurrent;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BenhaScooters.Infrastructure.Notifications;

public class SignalRConnectionTracker(IServiceScopeFactory scopeFactory) : ISignalRConnectionTracker
{
    public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<UserId, ConcurrentDictionary<string, byte>> _connectionsByUser = new();

    public Task TrackConnectedAsync(UserId userId, string connectionId)
    {
        var connections = _connectionsByUser.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));

        connections[connectionId] = 0;
        return Task.CompletedTask;
    }

    public Task TrackDisconnectedAsync(UserId userId, string connectionId)
    {
        if (!_connectionsByUser.TryGetValue(userId, out var connections))
            return Task.CompletedTask;

        connections.TryRemove(connectionId, out _);

        if (connections.IsEmpty)
        {
            _connectionsByUser.TryRemove(
                new KeyValuePair<UserId, ConcurrentDictionary<string, byte>>(userId, connections));
        }

        return Task.CompletedTask;
    }

    public async Task RecordHeartbeat(UserId userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.DriverStatuses.Where(ds => ds.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ds => ds.LastHeartbeat, DateTime.UtcNow));
    }

    public async Task<bool> IsConnectedAsync(UserId userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lastHeartbeat = await dbContext.DriverStatuses
            .AsNoTracking()
            .Where(ds => ds.UserId == userId)
            .Select(ds => ds.LastHeartbeat)
            .FirstOrDefaultAsync();

        if (lastHeartbeat is null)
            return false;

        return (DateTime.UtcNow - lastHeartbeat.Value) < HeartbeatTimeout;
    }

    public Task<bool> HasActiveConnectionAsync(UserId userId)
    {
        var hasActiveConnection = _connectionsByUser.TryGetValue(userId, out var connections)
            && !connections.IsEmpty;

        return Task.FromResult(hasActiveConnection);
    }
}
