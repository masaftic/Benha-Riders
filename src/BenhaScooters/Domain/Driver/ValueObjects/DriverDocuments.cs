using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Driver.ValueObjects;

public class DriverDocuments : ValueObject
{
    public string LicenseImageUrl { get; private set; } = null!;
    public string VehicleRegistrationImageUrl { get; private set; } = null!;
    public string ProfileImageUrl { get; private set; } = null!;

    private DriverDocuments() { } // For EF Core

    public DriverDocuments(string licenseImageUrl, string vehicleRegistrationImageUrl, string profileImageUrl)
    {
        if (string.IsNullOrWhiteSpace(licenseImageUrl))
            throw new ArgumentException("License image is required.", nameof(licenseImageUrl));

        if (string.IsNullOrWhiteSpace(vehicleRegistrationImageUrl))
            throw new ArgumentException("Vehicle registration image is required.", nameof(vehicleRegistrationImageUrl));

        if (string.IsNullOrWhiteSpace(profileImageUrl))
            throw new ArgumentException("Profile image is required.", nameof(profileImageUrl));

        LicenseImageUrl = licenseImageUrl;
        VehicleRegistrationImageUrl = vehicleRegistrationImageUrl;
        ProfileImageUrl = profileImageUrl;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LicenseImageUrl;
        yield return VehicleRegistrationImageUrl;
        yield return ProfileImageUrl;
    }
}
