using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BenhaScooters.Infrastructure.Notifications;

public class SignalRConnectionTracker(IServiceScopeFactory scopeFactory) : ISignalRConnectionTracker
{
    public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(40);

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
}
