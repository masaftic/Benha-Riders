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

public record GetOnboardingProgressQuery(DriverId DriverId) : IRequest<ErrorOr<GetOnboardingProgressResponse>>;

public record GetOnboardingProgressResponse(
    OnboardingStatus Status,
    OnboardingStep CurrentStep,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingProgressQuery, ErrorOr<GetOnboardingProgressResponse>>
{
    private readonly AppDbContext _db;

    public GetOnboardingStatusQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetOnboardingProgressResponse>> Handle(GetOnboardingProgressQuery request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var response = new GetOnboardingProgressResponse(
            driver.OnboardingState.Status,
            driver.OnboardingState.CurrentStep,
            driver.OnboardingProgress,
            driver.OnboardingState.RejectionReason,
            driver.OnboardingState.CreatedAt,
            driver.OnboardingState.CompletedAt);

        return response;
    }
}
