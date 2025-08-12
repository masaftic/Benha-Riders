using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record RejectDriverFieldCommand(
    DriverId DriverId, 
    string Step, 
    string FieldName, 
    string Reason) : IRequest<ErrorOr<Success>>;

public class RejectDriverFieldCommandHandler(AppDbContext db) : IRequestHandler<RejectDriverFieldCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(RejectDriverFieldCommand request, CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver is null)
        {
            return DriverErrors.DriverNotFound;
        }

        var result = driver.RejectField(request.Step, request.FieldName, request.Reason);
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
