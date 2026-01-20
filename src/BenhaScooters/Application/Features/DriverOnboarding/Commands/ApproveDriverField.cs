using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

/// <summary>
/// DEPRECATED: Field-level validation is being replaced with profile-level approval.
/// This command is kept for backward compatibility but is now a no-op.
/// </summary>
public record ApproveDriverFieldCommand(
    DriverId DriverId, 
    string Step, 
    string FieldName) : IRequest<ErrorOr<Success>>;

public class ApproveDriverFieldCommandHandler(AppDbContext db) : IRequestHandler<ApproveDriverFieldCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ApproveDriverFieldCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverProfile is null)
        {
            return DriverErrors.Profile.NotFound;
        }

        // Field-level validation is deprecated - this is now a no-op
        return Result.Success;
    }
}
