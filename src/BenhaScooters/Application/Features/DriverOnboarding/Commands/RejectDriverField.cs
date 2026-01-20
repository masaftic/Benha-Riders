using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

/// <summary>
/// DEPRECATED: Field-level rejection is being replaced with profile-level rejection.
/// This command is kept for backward compatibility but now rejects the entire profile.
/// </summary>
public record RejectDriverFieldCommand(
    DriverId DriverId, 
    string Step, 
    string FieldName, 
    string Reason) : IRequest<ErrorOr<Success>>;

public class RejectDriverFieldCommandHandler(AppDbContext db) : IRequestHandler<RejectDriverFieldCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(RejectDriverFieldCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverProfile is null)
        {
            return DriverErrors.Profile.NotFound;
        }

        // Reject the entire profile with the provided reason
        var result = driverProfile.Reject(request.Reason);
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
