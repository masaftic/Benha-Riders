using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Value object for driver personal information
/// </summary>
public class DriverPersonalInfo : ValueObject
{
    public string FullName { get; private set; } = null!;
    public NationalId NationalId { get; private set; } = null!;

    private DriverPersonalInfo() { } // For EF Core

    public DriverPersonalInfo(
        string fullName, 
        NationalId nationalId)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        FullName = fullName;
        NationalId = nationalId;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullName;
        yield return NationalId;
    }
}
