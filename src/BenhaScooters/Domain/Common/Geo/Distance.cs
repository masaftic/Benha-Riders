using Thinktecture;

namespace BenhaScooters.Domain.Common.Geo;


[ValueObject<double>(
    CreateFactoryMethodName = "FromMeters",
    TryCreateFactoryMethodName = "TryFromMeters")]
public partial struct Distance
{
    // inner value is in meters
    static partial void ValidateFactoryArguments(ref ValidationError? validationError, ref double value)
    {
        if (value < 0)
        {
            validationError = new ValidationError("Distance cannot be negative.");
        }
    }

    public static Distance FromKilometers(double kilometers) => new Distance(kilometers * 1000);

    public double ToMeters() => _value;
    public double ToKilometers() => _value / 1000;
}
