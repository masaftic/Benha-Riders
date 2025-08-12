using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record ApproveDriverFieldCommand(
    DriverId DriverId, 
    string Step, 
    string FieldName) : IRequest<ErrorOr<Success>>;

public class ApproveDriverFieldCommandHandler(AppDbContext db) : IRequestHandler<ApproveDriverFieldCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ApproveDriverFieldCommand request, CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver is null)
        {
            return DriverErrors.DriverNotFound;
        }

        var result = driver.ApproveField(request.Step, request.FieldName);
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
