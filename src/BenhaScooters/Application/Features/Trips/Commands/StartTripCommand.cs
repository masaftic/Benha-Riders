using BenhaScooters.Application.Common.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record StartTripCommand(
    UserId DriverId,
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

public class StartTripCommandHandler(
    AppDbContext db,
    IOptions<DriverWalletOptions> walletOptions,
    ILogger<StartTripCommandHandler> logger) : IRequestHandler<StartTripCommand, ErrorOr<StartTripResult>>
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

        // Charge wallet commission when trip starts
        var wallet = await db.DriverWallets
            .FirstOrDefaultAsync(w => w.DriverUserId == request.DriverId, cancellationToken);

        if (wallet != null)
        {
            var commissionPercentage = (decimal)walletOptions.Value.CommissionPercentage;
            var commission = Math.Round(trip.FinalFare.Amount * commissionPercentage / 100m, 2);
            
            var chargeResult = wallet.ChargeCommission(
                commission, 
                trip.Id, 
                $"Trip commission ({commissionPercentage}%)");
            
            if (chargeResult.IsError)
            {
                logger.LogWarning("Failed to charge wallet for trip {TripId}: {Errors}",
                    trip.Id.Value, string.Join(", ", chargeResult.Errors.Select(e => e.Description)));
                // Note: We continue even if charge fails - trip already started
            }
        }
        else
        {
            logger.LogWarning("Wallet not found for driver {DriverId} when starting trip {TripId}",
                request.DriverId.Value, trip.Id.Value);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new StartTripResult(
            trip.Id,
            "Trip started successfully",
            trip.AssignedAt);
    }
}
