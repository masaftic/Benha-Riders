using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.Trips.Queries.Common;

/// <summary>
/// Unified driver information used in both current trip queries and SignalR notifications
/// </summary>
public record DriverInfo(
    string Name, 
    PhoneNumber PhoneNumber,
    string? PhotoUrl,
    string VehicleModel,
    string VehicleBrand,
    string VehicleColor, 
    LicensePlate VehicleLicensePlate);
