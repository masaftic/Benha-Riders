namespace BenhaScooters.Application.Abstractions;

public interface IDriverNotifications
{
    Task NotifyDriverAsync(string driverId, string message);
    Task NotifyRideRequestOfferAsync(string driverId, string offerId);
    Task NotifyRideRequestOfferExpiredAsync(string driverId, string offerId);
}
