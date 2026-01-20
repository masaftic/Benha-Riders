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
            .Must(value => Enum.TryParse<DriverAvailabilityStatus>(value, ignoreCase: true, out _))
            .WithMessage("Invalid driver status");
    }
}

public record SetDriverAvailabilityResponse(
    DriverAvailabilityStatus Status,
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
        var userId = request.DriverId.ToUserId();
        
        // Get or create driver status
        var driverStatus = await _db.DriverStatuses
            .FirstOrDefaultAsync(ds => ds.UserId == userId, cancellationToken);

        if (driverStatus == null)
        {
            driverStatus = new DriverStatus(userId);
            await _db.DriverStatuses.AddAsync(driverStatus, cancellationToken);
        }

        // Update driver availability based on requested status
        string message;

        ErrorOr<Success> result;
        try
        {
            switch (Enum.Parse<DriverAvailabilityStatus>(request.DriverStatus, ignoreCase: true))
            {
                case DriverAvailabilityStatus.Online:
                    result = driverStatus.GoOnline();
                    message = "Driver is now online and available";
                    break;

                case DriverAvailabilityStatus.Offline:
                    result = driverStatus.GoOffline();
                    message = "Driver is now offline";
                    break;

                case DriverAvailabilityStatus.Busy:
                    result = driverStatus.SetBusy();
                    message = "Driver is busy and not accepting requests";
                    break;

                case DriverAvailabilityStatus.OnTrip:
                    return Error.Validation("INVALID_STATUS_TRANSITION", "Cannot manually set status to OnTrip. This status is set automatically when a trip starts.");

                default:
                    return Error.Validation("INVALID_STATUS", "Invalid status transition");
            }

            if (result.IsError) return result.Errors;

            await _db.SaveChangesAsync(cancellationToken);

            return new SetDriverAvailabilityResponse(
                driverStatus.Status,
                driverStatus.LastStatusChange,
                message);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("INVALID_OPERATION", ex.Message);
        }
    }
}
