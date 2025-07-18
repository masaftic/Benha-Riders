using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record DriverArrivedCommand(
    DriverId DriverId,
    TripId TripId) : IRequest<ErrorOr<DriverArrivedResult>>;

public record DriverArrivedResult(
    TripId TripId,
    string Message,
    DateTime ArrivedAt);

public class DriverArrivedCommandValidator : AbstractValidator<DriverArrivedCommand>
{
    public DriverArrivedCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
    }
}

public class DriverArrivedCommandHandler(AppDbContext db) : IRequestHandler<DriverArrivedCommand, ErrorOr<DriverArrivedResult>>
{
    public async Task<ErrorOr<DriverArrivedResult>> Handle(DriverArrivedCommand request, CancellationToken cancellationToken)
    {
        // Find the trip
        var trip = await db.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverId == request.DriverId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        // Mark driver as arrived
        trip.DriverArrived();
        await db.SaveChangesAsync(cancellationToken);

        return new DriverArrivedResult(
            trip.Id,
            "Driver marked as arrived successfully",
            trip.DriverArrivedAt!.Value);
    }
}
