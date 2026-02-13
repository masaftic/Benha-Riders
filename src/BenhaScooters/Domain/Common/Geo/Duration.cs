using Thinktecture;

namespace BenhaScooters.Domain.Common.Geo;

[ValueObject<double>(
    CreateFactoryMethodName = "FromSeconds",
    TryCreateFactoryMethodName = "TryFromSeconds")]
public partial struct Duration
{
    // inner value is in seconds
    static partial void ValidateFactoryArguments(ref ValidationError? validationError, ref double value)
    {
        if (value < 0)
        {
            validationError = new ValidationError("Duration cannot be negative.");
        }
    }

    public static Duration FromMinutes(double minutes) => new Duration(minutes * 60);

    public double ToSeconds() => _value;
    public double ToMinutes() => _value / 60;
}
