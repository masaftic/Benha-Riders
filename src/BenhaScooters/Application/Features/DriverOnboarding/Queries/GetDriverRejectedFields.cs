
using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;


public record GetDriverRejectedFieldsQuery(DriverId DriverId) : IRequest<ErrorOr<List<RejectedFieldDto>>>;


public class GetDriverRejectedFieldsQueryHandler(AppDbContext db) : IRequestHandler<GetDriverRejectedFieldsQuery, ErrorOr<List<RejectedFieldDto>>>
{
    public async Task<ErrorOr<List<RejectedFieldDto>>> Handle(GetDriverRejectedFieldsQuery request, CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver is null)
        {
            return DriverErrors.DriverNotFound;
        }

        if (driver.OnboardingState.Status != OnboardingStatus.Rejected)
        {
            return Error.Validation("INVALID_STATE_FOR_FIELD_RETRIEVAL", "Driver onboarding is not in a state that allows field retrieval.");
        }

        var rejectedFields = driver.Fields
            .Where(f => f.Status == FieldStatus.Rejected)
            .Select(f => new RejectedFieldDto(f.Step, f.FieldName, f.RejectionReason))
            .ToList();

        return rejectedFields;
    }
}
