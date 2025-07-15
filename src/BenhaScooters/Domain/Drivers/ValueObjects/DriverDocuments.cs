using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Drivers.ValueObjects;

public class DriverDocuments : ValueObject
{
    public string LicenseImageUrl { get; private set; } = null!;
    public string VehicleRegistrationImageUrl { get; private set; } = null!;
    public string ImageUrl { get; private set; } = null!;

    private DriverDocuments() { } // For EF Core

    public DriverDocuments(string licenseImageUrl, string vehicleRegistrationImageUrl, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(licenseImageUrl))
            throw new ArgumentException("License image is required.", nameof(licenseImageUrl));

        if (string.IsNullOrWhiteSpace(vehicleRegistrationImageUrl))
            throw new ArgumentException("Vehicle registration image is required.", nameof(vehicleRegistrationImageUrl));

        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException(" image is required.", nameof(imageUrl));

        LicenseImageUrl = licenseImageUrl;
        VehicleRegistrationImageUrl = vehicleRegistrationImageUrl;
        ImageUrl = imageUrl;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LicenseImageUrl;
        yield return VehicleRegistrationImageUrl;
        yield return ImageUrl;
    }
}
