using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using Microsoft.AspNetCore.Http;

namespace BenhaScooters.Contracts.DriverOnboarding;

public record UpdatePersonalInfoRequest(
    string FullName,
    string NationalId);

public record UpdateVehicleInfoRequest(
    VehicleType VehicleType,
    string VehicleBrand,
    string VehicleColor,
    string LicensePlate,
    int VehicleYear);

public record UpdateDocumentsRequest(
    IFormFile LicenseImage,
    IFormFile VehicleRegistrationImage,
    IFormFile DriverImage);

public record UploadDocumentRequest(
    string DocumentType,
    IFormFile File);

public record BanDriverRequest(string Reason);

public record QueryDriversParams(DriverOnboardingStatus? OnboardingStatus, int Page = 1, int PageCount = 10);
