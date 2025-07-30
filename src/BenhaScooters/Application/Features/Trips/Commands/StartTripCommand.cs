using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record StartTripCommand(
    DriverId DriverId,
    TripId TripId) : IRequest<ErrorOr<StartTripResult>>;

public record StartTripResult(
    TripId TripId,
    string Message,
    DateTime StartedAt);

public class StartTripCommandValidator : AbstractValidator<StartTripCommand>
{
    public StartTripCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripId.Value)
            .NotEmpty()
            .WithMessage("Trip ID is required");
    }
}

public class StartTripCommandHandler(AppDbContext db) : IRequestHandler<StartTripCommand, ErrorOr<StartTripResult>>
{
    public async Task<ErrorOr<StartTripResult>> Handle(StartTripCommand request, CancellationToken cancellationToken)
    {
        // Find the trip
        var trip = await db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverId == request.DriverId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        // Start the trip
        var result = trip.StartTrip();
        if (result.IsError)
        {
            return result.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new StartTripResult(
            trip.Id,
            "Trip started successfully",
            trip.AssignedAt);
    }
}
