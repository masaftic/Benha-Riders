using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record ApproveDriverStepCommand(
    DriverId DriverId, 
    string Step) : IRequest<ErrorOr<Success>>;

public class ApproveDriverStepCommandHandler(AppDbContext db) : IRequestHandler<ApproveDriverStepCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ApproveDriverStepCommand request, CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver is null)
        {
            return DriverErrors.DriverNotFound;
        }

        var result = driver.ApproveEntireStep(request.Step);
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
