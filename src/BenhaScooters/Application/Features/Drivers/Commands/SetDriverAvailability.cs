using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Commands;

public record SetDriverAvailabilityCommand(DriverId DriverId, string DriverStatus) : IRequest<ErrorOr<SetDriverAvailabilityResponse>>;

public class SetDriverAvailabilityCommandValidator : AbstractValidator<SetDriverAvailabilityCommand>
{
    public SetDriverAvailabilityCommandValidator()
    {
        RuleFor(x => x.DriverStatus)
            .Must(value => Enum.TryParse<DriverStatus>(value, ignoreCase: true, out _))
            .WithMessage("Invalid driver status");
    }
}

public record SetDriverAvailabilityResponse(
    DriverStatus Status,
    DateTime LastStatusChange,
    string Message);

public class SetDriverAvailabilityCommandHandler : IRequestHandler<SetDriverAvailabilityCommand, ErrorOr<SetDriverAvailabilityResponse>>
{
    private readonly AppDbContext _db;

    public SetDriverAvailabilityCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<SetDriverAvailabilityResponse>> Handle(SetDriverAvailabilityCommand request, CancellationToken cancellationToken)
    {
        // Get or create driver availability
        var availability = await _db.DriverAvailabilities
            .FirstOrDefaultAsync(da => da.DriverId == request.DriverId, cancellationToken);

        if (availability == null)
        {
            availability = new DriverAvailability(request.DriverId);
            await _db.DriverAvailabilities.AddAsync(availability, cancellationToken);
        }

        // Update driver availability based on requested status
        string message;

        ErrorOr<Success> result;
        try
        {
            switch (Enum.Parse<DriverStatus>(request.DriverStatus, ignoreCase: true))
            {
                case DriverStatus.Online:
                    result = availability.GoOnline();
                    message = "Driver is now online and available";
                    break;

                case DriverStatus.Offline:
                    result = availability.GoOffline();
                    message = "Driver is now offline";
                    break;

                case DriverStatus.Busy:
                    result = availability.SetBusy();
                    message = "Driver is busy and not accepting requests";
                    break;

                case DriverStatus.OnTrip:
                    return Error.Validation("INVALID_STATUS_TRANSITION", "Cannot manually set status to OnTrip. This status is set automatically when a trip starts.");

                default:
                    return Error.Validation("INVALID_STATUS", "Invalid status transition");
            }

            if (result.IsError) return result.Errors;

            await _db.SaveChangesAsync(cancellationToken);

            return new SetDriverAvailabilityResponse(
                availability.Status,
                availability.LastStatusChange,
                message);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("INVALID_OPERATION", ex.Message);
        }
    }
}
