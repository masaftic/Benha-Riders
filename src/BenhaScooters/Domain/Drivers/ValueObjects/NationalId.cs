using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using Thinktecture;


namespace BenhaScooters.Domain.Drivers.ValueObjects;

[ValueObject<string>]
[KeyMemberEqualityComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
[KeyMemberComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
public partial class NationalId
{
    static partial void ValidateFactoryArguments(
        ref ValidationError? validationError,
        ref string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationError = new ValidationError("الرقم القومي مطلوب.");
            return;
        }

        if (value.Length != 14 || !value.All(char.IsDigit))
        {
            validationError = new ValidationError("يجب أن يكون الرقم القومي مكونًا من 14 رقمًا بالضبط.");
            return;
        }
    }
}
