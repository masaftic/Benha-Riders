using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Driver.Enums;
using Vogen;

namespace BenhaScooters.Domain.Driver.ValueObjects;

[ValueObject<string>]
public partial struct LicensePlate
{
    private static Validation Validate(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
            return Validation.Invalid("License plate cannot be empty.");

        // Basic Egyptian license plate validation
        if (licensePlate.Length < 3 || licensePlate.Length > 10)
            return Validation.Invalid("License plate must be between 3 and 10 characters.");

        return Validation.Ok;
    }
}

public class VehicleInfo : ValueObject
{
    public VehicleType VehicleType { get; private set; }
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public LicensePlate LicensePlate { get; private set; }
    public int Year { get; private set; }

    private VehicleInfo() { } // For EF Core

    public VehicleInfo(VehicleType vehicleType, string brand, string model,
        string color, LicensePlate licensePlate, int year)
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
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VehicleType;
        yield return Brand;
        yield return Model;
        yield return Color;
        yield return LicensePlate;
        yield return Year;
    }
}
