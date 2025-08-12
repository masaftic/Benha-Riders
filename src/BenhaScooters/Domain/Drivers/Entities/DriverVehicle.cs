using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Vogen;

namespace BenhaScooters.Domain.Drivers.Entities;

[ValueObject<int>]
public partial struct DriverVehicleId;

[ValueObject<string>]
public partial struct VIN
{
    private static Validation Validate(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return Validation.Invalid("VIN cannot be empty.");

        // Basic VIN validation (17 characters for modern vehicles)
        if (vin.Length != 17)
            return Validation.Invalid("VIN must be 17 characters.");

        return Validation.Ok;
    }
}

public class DriverVehicle
{
    public DriverVehicleId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public LicensePlate LicensePlate { get; private set; }
    public int Year { get; private set; }
    public VIN VIN { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    // Navigation property
    public Driver Driver { get; private set; } = null!;

    private DriverVehicle() { } // For EF Core

    public DriverVehicle(DriverId driverId, VehicleType vehicleType, string brand, string model,
        string color, LicensePlate licensePlate, int year, VIN vin)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Vehicle brand is required.", nameof(brand));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Vehicle model is required.", nameof(model));

        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Vehicle color is required.", nameof(color));

        if (year < 1980 || year > DateTime.Now.Year + 1)
            throw new ArgumentException("Invalid vehicle year.", nameof(year));

        DriverId = driverId;
        VehicleType = vehicleType;
        Brand = brand;
        Model = model;
        Color = color;
        LicensePlate = licensePlate;
        Year = year;
        VIN = vin;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        DeactivatedAt = null;
    }

    public string GetDisplayName() => $"{Brand} {Model} ({Year})";

    public ErrorOr<Success> UpdateField(string name, string value)
    {
        var pascalName = ToPascalCase(name);

        switch (pascalName)
        {
            case nameof(Brand):
                Brand = value;
                break;
            case nameof(Model):
                Model = value;
                break;
            case nameof(Color):
                Color = value;
                break;
            case nameof(LicensePlate):
                LicensePlate = LicensePlate.From(value);
                break;
            case nameof(Year):
                if (int.TryParse(value, out var year))
                    Year = year;
                else
                    return Error.Validation("INVALID_YEAR", "Invalid year format.");
                break;
            case nameof(VIN):
                VIN = VIN.From(value);
                break;
            default:
                return Error.Validation("UNKNOWN_FIELD_NAME", $"Unknown field: {name}");
        }

        return Result.Success;
    }


    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input[1..];
    }
}
