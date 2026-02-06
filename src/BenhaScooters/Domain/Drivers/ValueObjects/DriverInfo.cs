using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using Thinktecture;


namespace BenhaScooters.Domain.Drivers.ValueObjects;

[ValueObject<string>]
[KeyMemberEqualityComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
public partial class NationalId
{
    static partial void ValidateFactoryArguments(
        ref ValidationError? validationError,
        ref string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationError = new ValidationError("National ID cannot be empty.");
            return;
        }

        if (value.Length != 14 || !value.All(char.IsDigit))
        {
            validationError = new ValidationError("National ID must be exactly 14 digits.");
            return;
        }
    }
}


public class DriverInfo : ValueObject
{
    public string FullName { get; private set; } = null!;
    public NationalId NationalId { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public PhoneNumber EmergencyContactPhone { get; private set; }

    private DriverInfo() { } // For EF Core

    public DriverInfo(string fullName, NationalId nationalId, DateOnly dateOfBirth,
        string address, string city, string emergencyContactName, PhoneNumber emergencyContactPhone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        if (string.IsNullOrWhiteSpace(emergencyContactName))
            throw new ArgumentException("Emergency contact name is required.", nameof(emergencyContactName));

        FullName = fullName;
        NationalId = nationalId;
        DateOfBirth = dateOfBirth;
        Address = address;
        City = city;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullName;
        yield return NationalId;
        yield return DateOfBirth;
        yield return Address;
        yield return City;
        yield return EmergencyContactName;
        yield return EmergencyContactPhone;
    }

    public ErrorOr<Success> UpdateField(string name, string value)
    {
        var pascalName = ToPascalCase(name);

        switch (pascalName)
        {
            case nameof(FullName):
                FullName = value;
                break;
            case nameof(NationalId):
                NationalId = NationalId.Create(value);
                break;
            case nameof(DateOfBirth):
                DateOfBirth = DateOnly.Parse(value);
                break;
            case nameof(Address):
                Address = value;
                break;
            case nameof(City):
                City = value;
                break;
            case nameof(EmergencyContactName):
                EmergencyContactName = value;
                break;
            case nameof(EmergencyContactPhone):
                EmergencyContactPhone = PhoneNumber.Create(value);
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
