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
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingDetailsQuery(UserId DriverId) : IRequest<ErrorOr<GetOnboardingDetailsResponse>>;

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
        var driverProfile = await _db.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(dp => dp.UserId == request.DriverId, cancellationToken);

        if (driverProfile == null)
        {
            return DriverErrors.Profile.NotFound;
        }

        var personalInfo = driverProfile.PersonalInfo != null ? new PersonalInfoDto(
            driverProfile.PersonalInfo.FullName,
            driverProfile.PersonalInfo.NationalId,
            driverProfile.User.PhoneNumber!,
            driverProfile.PersonalInfo.DateOfBirth,
            driverProfile.PersonalInfo.Address,
            driverProfile.PersonalInfo.City,
            driverProfile.PersonalInfo.EmergencyContactName,
            driverProfile.PersonalInfo.EmergencyContactPhone) : null;

        var vehicleInfo = driverProfile.Vehicle != null ? new VehicleInfoDto(
            driverProfile.Vehicle.VehicleType,
            driverProfile.Vehicle.Brand,
            driverProfile.Vehicle.Model,
            driverProfile.Vehicle.Color,
            driverProfile.Vehicle.LicensePlate,
            driverProfile.Vehicle.Year,
            driverProfile.Vehicle.VIN,
            true, // Vehicle is part of profile, always active
            driverProfile.CreatedAt) : null;


        var documentTasks = driverProfile.Documents.Select(doc => ToDocumentDto(doc, _s3));
        List<DocumentDto> documents = [.. await Task.WhenAll(documentTasks)];

        var isComplete = driverProfile.IsComplete;
        var progress = CalculateProgress(driverProfile);
        
        var onboardingState = new OnboardingStateDto(
            driverProfile.OnboardingStatus.ToString(),
            progress,
            driverProfile.RejectionReason,
            driverProfile.CreatedAt,
            driverProfile.ApprovedAt,
            driverProfile.OnboardingStatus == DriverOnboardingStatus.Approved);

        var response = new GetOnboardingDetailsResponse(
            onboardingState,
            personalInfo,
            vehicleInfo,
            documents);

        return response;
    }
    
    private static int CalculateProgress(DriverProfile profile)
    {
        int steps = 0;
        if (profile.PersonalInfo != null) steps++;
        if (profile.Vehicle != null) steps++;
        if (profile.Documents.Count >= 3) steps++;
        if (profile.OnboardingStatus == DriverOnboardingStatus.Approved) steps++;
        return steps * 25; // 25% per step
    }
    
    private static async Task<DocumentDto> ToDocumentDto(DriverDocument doc, IS3Service s3)
    {
        var url = await s3.GetPreSignedUrlAsync(doc.ImageUrl, TimeSpan.FromMinutes(15));
        return new DocumentDto(doc.Type.ToString(), url, doc.UploadedAt, doc.ExpiryDate);
    }
}
