using Thinktecture;


namespace BenhaScooters.Domain.Drivers.ValueObjects;

[ValueObject<string>]
[KeyMemberEqualityComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
[KeyMemberComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
public partial class LicensePlate
{
    static partial void ValidateFactoryArguments(
        ref ValidationError? validationError,
        ref string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationError = new ValidationError("License plate cannot be empty.");
            return;
        }

        if (value.Length < 3 || value.Length > 10)
        {
            validationError = new ValidationError(
                "License plate must be between 3 and 10 characters.");
            return;
        }
    }
}