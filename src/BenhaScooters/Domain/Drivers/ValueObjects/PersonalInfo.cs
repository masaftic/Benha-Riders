using BenhaScooters.Domain.Common;
using Vogen;

namespace BenhaScooters.Domain.Drivers.ValueObjects;


[ValueObject<string>]
public partial struct NationalId
{
    private static Validation Validate(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return Validation.Invalid("National ID cannot be empty.");

        // Egyptian national ID validation (14 digits)
        if (!System.Text.RegularExpressions.Regex.IsMatch(nationalId, @"^[0-9]{14}$"))
            return Validation.Invalid("National ID must be 14 digits.");

        return Validation.Ok;
    }
}


public class PersonalInfo : ValueObject
{
    public string FullName { get; private set; } = null!;
    public NationalId NationalId { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public Users.PhoneNumber EmergencyContactPhone { get; private set; }

    private PersonalInfo() { } // For EF Core

    public PersonalInfo(string fullName, NationalId nationalId, DateOnly dateOfBirth,
        string address, string city, string emergencyContactName, Users.PhoneNumber emergencyContactPhone)
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
}
