namespace BenhaScooters.Contracts.Drivers;

public record SetDriverAvailabilityRequest(string Status);

public record UpdateLocationRequest(double Latitude, double Longitude);
