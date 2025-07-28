using Vogen;

namespace BenhaScooters.Domain.Drivers.ValueObjects;

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
