using Thinktecture;

namespace BenhaScooters.Domain.Common.Geo;

[ValueObject<double>(
    CreateFactoryMethodName = "FromMinutes",
    TryCreateFactoryMethodName = "TryFromMinutes")]
public partial struct Duration
{
    // inner value is in minutes
    static partial void ValidateFactoryArguments(ref ValidationError? validationError, ref double value)
    {
        if (value < 0)
        {
            validationError = new ValidationError("Duration cannot be negative.");
        }
    }

    public static Duration FromSeconds(double seconds) => new Duration(seconds / 60);

    public double ToMinutes() => _value;
    public double ToSeconds() => _value * 60;
}
