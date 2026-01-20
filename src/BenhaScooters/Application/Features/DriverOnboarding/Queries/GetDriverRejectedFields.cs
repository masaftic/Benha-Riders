
using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

/// <summary>
/// Gets rejection reason for a driver profile.
/// Simplified from field-level to profile-level rejection.
/// </summary>
public record GetDriverRejectionReasonQuery(DriverId DriverId) : IRequest<ErrorOr<RejectionReasonDto>>;

public record RejectionReasonDto(string? Reason);

public class GetDriverRejectionReasonQueryHandler(AppDbContext db) : IRequestHandler<GetDriverRejectionReasonQuery, ErrorOr<RejectionReasonDto>>
{
    public async Task<ErrorOr<RejectionReasonDto>> Handle(GetDriverRejectionReasonQuery request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverProfile is null)
        {
            return DriverErrors.Profile.NotFound;
        }

        if (driverProfile.OnboardingStatus != DriverOnboardingStatus.Rejected)
        {
            return Error.Validation("INVALID_STATE", "Driver profile is not in rejected state.");
        }

        return new RejectionReasonDto(driverProfile.RejectionReason);
    }
}
