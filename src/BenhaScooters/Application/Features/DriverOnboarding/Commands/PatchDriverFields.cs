using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record FieldDto(string Step, string FieldName, string Value);

/// <summary>
/// DEPRECATED: Field-level patching is being replaced with full profile updates.
/// This command is kept for backward compatibility but is now a no-op.
/// </summary>
public record PatchDriverFieldsCommand(
    DriverId DriverId, List<FieldDto> Fields) : IRequest<ErrorOr<Success>>;

public class PatchDriverFieldsCommandHandler(AppDbContext db) : IRequestHandler<PatchDriverFieldsCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(PatchDriverFieldsCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverProfile is null)
        {
            return DriverErrors.Profile.NotFound;
        }

        // Field-level patching is deprecated
        // Use specific update commands (UpdatePersonalInfo, UpdateVehicle, etc.)
        return Result.Success;
    }
}
