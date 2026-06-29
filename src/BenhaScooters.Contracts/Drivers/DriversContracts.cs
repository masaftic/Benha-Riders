namespace BenhaScooters.Contracts.Drivers;

public enum AvailabilityStatus
{
    Offline = 0,
    Online = 1
}

public record SetDriverAvailabilityRequest(AvailabilityStatus Status);

public record UpdateLocationRequest(double Latitude, double Longitude);
