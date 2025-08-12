using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record FieldDto(string Step, string FieldName, string Value);

public record PatchDriverFieldsCommand(
    DriverId DriverId, List<FieldDto> Fields) : IRequest<ErrorOr<Success>>;

public class PatchDriverFieldsCommandHandler(AppDbContext db) : IRequestHandler<PatchDriverFieldsCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(PatchDriverFieldsCommand request, CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .Include(d => d.Info)
            .Include(d => d.Vehicle)
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver is null)
        {
            return DriverErrors.DriverNotFound;
        }

        if (driver.OnboardingState.Status == OnboardingStatus.Completed)
        {
            return DriverErrors.OnboardingAlreadyCompleted;
        }

        if (driver.OnboardingState.Status != OnboardingStatus.Rejected)
        {
            return Error.Validation("INVALID_STATE_FOR_FIELD_CHANGE", "Driver onboarding is not in a state that allows field updates.");
        }

        List<Error> errors = [];

        foreach (var patch in request.Fields)
        {
            var result = driver.PatchField(patch.Step, patch.FieldName, patch.Value);
            if (result.IsError)
            {
                errors.AddRange(result.Errors);
            }
        }

        if (errors.Count > 0) return errors;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
