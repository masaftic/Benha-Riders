using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

/// <summary>
/// DEPRECATED: Field-level validation is being replaced with profile-level approval.
/// This command is kept for backward compatibility but now approves the entire driver profile.
/// </summary>
public record ApproveDriverStepCommand(
    DriverId DriverId, 
    string Step) : IRequest<ErrorOr<Success>>;

public class ApproveDriverStepCommandHandler(AppDbContext db) : IRequestHandler<ApproveDriverStepCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ApproveDriverStepCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverProfile is null)
        {
            return DriverErrors.Profile.NotFound;
        }

        // Field-level validation is deprecated - this is now a no-op
        // Use ApproveDriverCommand for full approval
        return Result.Success;
    }
}
