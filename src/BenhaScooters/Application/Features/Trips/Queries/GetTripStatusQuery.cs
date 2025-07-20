using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetTripStatusQuery(
    RiderId RiderId,
    TripRequestId TripRequestId) : IRequest<ErrorOr<GetTripStatusResult>>;

public record GetTripStatusResult(
    TripRequestId TripRequestId,
    TripRequestStatus Status,
    string? AssignedDriverName,
    DateTime RequestedAt,
    DateTime? AcceptedAt,
    DateTime? ExpiresAt,
    string? CancellationReason);

public class GetTripStatusQueryValidator : AbstractValidator<GetTripStatusQuery>
{
    public GetTripStatusQueryValidator()
    {
        RuleFor(x => x.RiderId.Value)
            .NotEmpty()
            .WithMessage("Rider ID is required");

        RuleFor(x => x.TripRequestId.Value)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class GetTripStatusQueryHandler(AppDbContext db) : IRequestHandler<GetTripStatusQuery, ErrorOr<GetTripStatusResult>>
{
    public async Task<ErrorOr<GetTripStatusResult>> Handle(GetTripStatusQuery request, CancellationToken cancellationToken)
    {
        // Find the trip request
        var tripRequest = await db.TripRequests
            .Include(tr => tr.AssignedDriver)
            .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId && tr.RiderId == request.RiderId, cancellationToken);

        if (tripRequest == null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        var driverName = tripRequest.AssignedDriver?.PersonalInfo?.FullName;

        return new GetTripStatusResult(
            tripRequest.Id,
            tripRequest.Status,
            driverName,
            tripRequest.RequestedAt,
            tripRequest.AssignedAt,
            tripRequest.ExpiresAt,
            tripRequest.CancellationReason);
    }
}
