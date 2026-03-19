using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Abstractions;

public interface IPushNotificationService
{
    Task SendToUserAsync(UserId userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
}
