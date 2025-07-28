using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingStatusQuery(UserId UserId) : IRequest<ErrorOr<GetOnboardingStatusResponse>>;

public record GetOnboardingStatusResponse(
    OnboardingStatus Status,
    OnboardingStep CurrentStep,
    int Progress,
    string? RejectionReason,
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
            .FirstOrDefaultAsync(dp => dp.UserId == request.UserId, cancellationToken);

        if (driver == null)
        {
            // Create new driver if it doesn't exist
            driver = new Driver(request.UserId);
            _db.Drivers.Add(driver);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var response = new GetOnboardingStatusResponse(
            driver.OnboardingState.Status,
            driver.OnboardingState.CurrentStep,
            driver.OnboardingProgress,
            driver.OnboardingState.RejectionReason,
            driver.OnboardingState.CreatedAt,
            driver.OnboardingState.CompletedAt);

        return response;
    }
}
