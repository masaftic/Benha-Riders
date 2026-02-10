using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using Humanizer;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;

public record DriverSummaryDto(
    UserId Id,
    string? FullName,
    NationalId? NationalId,
    PhoneNumber PhoneNumber,
    string? VehicleBrand,
    int? VehicleYear,
    DriverOnboardingStatus Status,
    int Progress,
    DateTime CreatedAt);

public record PersonalInfoDto(
    string FullName,
    string NationalId,
    PhoneNumber PhoneNumber);

public record VehicleInfoDto(
    VehicleType VehicleType,
    string Brand,
    string Model,
    string Color,
    string LicensePlate,
    int Year,
    bool IsActive,
    DateTime CreatedAt);

public record DocumentDto(
    string Type,
    string ImageUrl,
    DateTime UploadedAt,
    DateOnly? ExpiryDate);


public record OnboardingStateDto(
    string Status,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool IsCompleted);

public record RejectedFieldDto(string Step, string FieldName, string? RejectionReason);
