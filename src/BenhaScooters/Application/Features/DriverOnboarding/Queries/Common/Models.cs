using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using Humanizer;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;

public record DriverSummaryDto(
    DriverId Id,
    string? FullName,
    NationalId? NationalId,
    PhoneNumber PhoneNumber,
    string? VehicleBrand,
    int? VehicleYear,
    OnboardingStatus Status,
    int Progress,
    DateTime CreatedAt);

public record PersonalInfoDto(
    string FullName,
    string NationalId,
    PhoneNumber PhoneNumber,
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
    DateOnly? ExpiryDate,
    string? RejectionReason);


public record OnboardingStateDto(
    string Status,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool IsCompleted);