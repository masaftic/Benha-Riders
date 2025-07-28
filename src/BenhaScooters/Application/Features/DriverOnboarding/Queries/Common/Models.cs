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
    string Brand,
    string Model,
    string Color,
    string LicensePlate,
    int Year,
    string VIN,
    bool IsActive,
    DateTime CreatedAt);

public record DocumentDto(
    string Type,
    string Status,
    string ImageUrl,
    DateTime UploadedAt,
    DateTime? ExpiryDate,
    string? RejectionReason,
    bool IsValid,
    bool IsExpired);

public record DocumentsDto(
    DocumentDto? DrivingLicense,
    DocumentDto? VehicleRegistration,
    DocumentDto? DriverPhoto);

public record OnboardingStateDto(
    string Status,
    string CurrentStep,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool IsCompleted,
    bool CanAdvance);