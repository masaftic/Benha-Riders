using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingStatusQuery(DriverId DriverId) : IRequest<ErrorOr<GetOnboardingStatusResponse>>;

public record GetOnboardingStatusResponse(
    DriverOnboardingStatus Status,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ApprovedAt);

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, ErrorOr<GetOnboardingStatusResponse>>
{
    private readonly AppDbContext _db;

    public GetOnboardingStatusQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetOnboardingStatusResponse>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return DriverErrors.Profile.NotFound;
        }

        var progress = CalculateProgress(driverProfile);

        var response = new GetOnboardingStatusResponse(
            driverProfile.OnboardingStatus,
            progress,
            driverProfile.RejectionReason,
            driverProfile.CreatedAt,
            driverProfile.ApprovedAt);

        return response;
    }
    
    private static int CalculateProgress(DriverProfile profile)
    {
        int steps = 0;
        if (profile.PersonalInfo != null) steps++;
        if (profile.Vehicle != null) steps++;
        if (profile.Documents.Count >= 3) steps++;
        if (profile.OnboardingStatus == DriverOnboardingStatus.Approved) steps++;
        return steps * 25;
    }
}
