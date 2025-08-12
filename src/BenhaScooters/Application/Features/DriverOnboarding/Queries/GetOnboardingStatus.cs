using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingStatusQuery(DriverId DriverId) : IRequest<ErrorOr<GetOnboardingStatusResponse>>;

public record GetOnboardingStatusResponse(
    OnboardingStatus Status,
    int Progress,
    List<RejectedFieldDto> RejectedFields,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, ErrorOr<GetOnboardingStatusResponse>>
{
    private readonly AppDbContext _db;

    public GetOnboardingStatusQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetOnboardingStatusResponse>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var rejectedFields = driver.Fields
            .Where(f => f.Status == FieldStatus.Rejected)
            .Select(f => new RejectedFieldDto(f.Step, f.FieldName, f.RejectionReason))
            .ToList();

        var response = new GetOnboardingStatusResponse(
            driver.OnboardingState.Status,
            driver.OnboardingProgress,
            rejectedFields,
            driver.OnboardingState.CreatedAt,
            driver.OnboardingState.CompletedAt);

        return response;
    }
}
