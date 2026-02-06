using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.Trips.Queries.Common;

public record DriverInfo(
    string Name, 
    PhoneNumber PhoneNumber, 
    string VehicleModel, 
    string VehicleColor, 
    LicensePlate VehicleLicensePlate);
