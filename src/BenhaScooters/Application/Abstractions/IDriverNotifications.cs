namespace BenhaScooters.Application.Abstractions;

public interface IDriverNotifications
{
    Task NotifyDriver(string driverId, string message);
    Task NotifyRideRequestOffer(string driverId, string offerId);
    Task NotifyRideRequestOfferExpired(string driverId, string offerId);
}
