using BenhaScooters.Domain.Drivers.Enums;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;

public record PersonalInfoDto(
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Address,
    string City,
    string EmergencyContactName,
    string EmergencyContactPhone);

public record VehicleInfoDto(
    VehicleType VehicleType,
    string VehicleBrand,
    string VehicleModel,
    string VehicleColor,
    string LicensePlate,
    int VehicleYear);

public record DocumentsDto(
    string LicenseImageUrl,
    string VehicleRegistrationImageUrl,
    string ImageUrl);