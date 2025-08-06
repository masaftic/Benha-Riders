using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record ListDriversWithFilters(
    OnboardingStatus OnboardingStatus,
    OnboardingStep OnboardingStep,
    int Page,
    int PageSize) : IRequest<ErrorOr<List<DriverDto>>>;

public record DriverDto(
    DriverId Id,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    List<DocumentDto> Documents,
    OnboardingStateDto OnboardingState,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    bool IsCompleted,
    bool CanAdvance);