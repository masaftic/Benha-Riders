using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingDetailsQuery(DriverId DriverId) : IRequest<ErrorOr<GetOnboardingDetailsResponse>>;

public record GetOnboardingDetailsResponse(
    OnboardingStateDto OnboardingState,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    List<DocumentDto> Documents);


public class GetOnboardingDetailsQueryHandler : IRequestHandler<GetOnboardingDetailsQuery, ErrorOr<GetOnboardingDetailsResponse>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3;

    public GetOnboardingDetailsQueryHandler(AppDbContext db, IS3Service s3)
    {
        _db = db;
        _s3 = s3;
    }

    public async Task<ErrorOr<GetOnboardingDetailsResponse>> Handle(GetOnboardingDetailsQuery request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers // Use Projection to avoid loading unnecessary data
            .Include(d => d.User)
            .Include(d => d.Documents)
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var personalInfo = driver.Info != null ? new PersonalInfoDto(
            driver.Info.FullName,
            driver.Info.NationalId.Value,
            driver.User.PhoneNumber!.Value,
            driver.Info.DateOfBirth,
            driver.Info.Address,
            driver.Info.City,
            driver.Info.EmergencyContactName,
            driver.Info.EmergencyContactPhone.Value) : null;

        var activeVehicle = driver.Vehicle;
        var vehicleInfo = activeVehicle != null ? new VehicleInfoDto(
            activeVehicle.VehicleType,
            activeVehicle.Brand,
            activeVehicle.Model,
            activeVehicle.Color,
            activeVehicle.LicensePlate.Value,
            activeVehicle.Year,
            activeVehicle.VIN.Value,
            activeVehicle.IsActive,
            activeVehicle.CreatedAt) : null;


        var documentTasks = driver.Documents.Select(x => x.ToDto(_s3));
        List<DocumentDto> documents = [.. await Task.WhenAll(documentTasks)];

        var onboardingState = new OnboardingStateDto(
            driver.OnboardingState.Status.ToString(),
            driver.OnboardingProgress,
            driver.OnboardingState.BanReason,
            driver.OnboardingState.CreatedAt,
            driver.OnboardingState.CompletedAt,
            driver.OnboardingState.IsCompleted);

        var response = new GetOnboardingDetailsResponse(
            onboardingState,
            personalInfo,
            vehicleInfo,
            documents);

        return response;
    }
}
