using Thinktecture;

namespace BenhaScooters.Domain.Common.Geo;


[ValueObject<double>(
    CreateFactoryMethodName = "FromKilometers",
    TryCreateFactoryMethodName = "TryFromKilometers")]
public partial struct Distance
{
    // inner value is in kilometers
    static partial void ValidateFactoryArguments(ref ValidationError? validationError, ref double value)
    {
        if (value < 0)
        {
            validationError = new ValidationError("Distance cannot be negative.");
        }
    }

    public static Distance FromMeters(double meters) => new Distance(meters / 1000);

    public double ToKilometers() => _value;
    public double ToMeters() => _value * 1000;
}
