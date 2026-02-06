using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Value object for driver vehicle information
/// </summary>
public class DriverVehicleInfo : ValueObject
{
    public VehicleType VehicleType { get; private set; }
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public LicensePlate LicensePlate { get; private set; }
    public int Year { get; private set; }
    public VIN VIN { get; private set; }

    private DriverVehicleInfo() { } // For EF Core

    public DriverVehicleInfo(
        VehicleType vehicleType,
        string brand,
        string model,
        string color,
        LicensePlate licensePlate,
        int year,
        VIN vin)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Vehicle brand is required.", nameof(brand));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Vehicle model is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Vehicle color is required.", nameof(color));
        if (year < 1980 || year > DateTime.Now.Year + 1)
            throw new ArgumentException("Invalid vehicle year.", nameof(year));

        VehicleType = vehicleType;
        Brand = brand;
        Model = model;
        Color = color;
        LicensePlate = licensePlate;
        Year = year;
        VIN = vin;
    }

    public string DisplayName => $"{Brand} {Model} ({Year})";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VehicleType;
        yield return Brand;
        yield return Model;
        yield return Color;
        yield return LicensePlate;
        yield return Year;
        yield return VIN;
    }
}
