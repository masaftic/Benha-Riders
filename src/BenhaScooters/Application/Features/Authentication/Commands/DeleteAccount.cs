using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record DeleteAccountCommand(UserId UserId) : IRequest<ErrorOr<Success>>;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, ErrorOr<Success>>
{
    private static readonly TripStatus[] ActiveTripStatuses =
    [
        TripStatus.Assigned,
        TripStatus.DriverArrived,
        TripStatus.InProgress
    ];

    private readonly AppDbContext _db;

    public DeleteAccountCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var hasActiveTrip = await _db.Trips
            .AnyAsync(
                t => (t.DriverId == request.UserId || t.RiderId == request.UserId)
                  && ActiveTripStatuses.Contains(t.Status),
                cancellationToken);

        if (hasActiveTrip)
        {
            return AppErrors.User.CannotDeleteWhileTripActive();
        }

        var hasPendingTripRequest = await _db.TripRequests
            .AnyAsync(
                tr => tr.RiderId == request.UserId
                   && tr.Status == TripRequestStatus.Pending
                   && tr.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (hasPendingTripRequest)
        {
            return AppErrors.User.CannotDeleteWithPendingTripRequest();
        }

        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.ExternalAuths)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return AppErrors.User.NotFound();
        }

        if (!user.IsActive)
        {
            return AppErrors.User.AccountDeactivated();
        }

        var riderProfile = await _db.RiderProfiles
            .Include(r => r.SavedAddresses)
            .FirstOrDefaultAsync(r => r.UserId == request.UserId, cancellationToken);

        var driverProfile = await _db.DriverProfiles
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.UserId == request.UserId, cancellationToken);

        var driverStatus = await _db.DriverStatuses
            .FirstOrDefaultAsync(d => d.UserId == request.UserId, cancellationToken);

        var deviceTokens = await _db.UserDeviceTokens
            .Where(t => t.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        _db.UserDeviceTokens.RemoveRange(deviceTokens);
        _db.ExternalAuths.RemoveRange(user.ExternalAuths);

        if (driverStatus is not null && driverStatus.Status != DriverAvailabilityStatus.OnTrip)
        {
            driverStatus.UpdateStatus(DriverAvailabilityStatus.Offline);
        }

        riderProfile?.Anonymize(User.DeletedAccountName);
        driverProfile?.Anonymize(user.Id);

        user.DeactivateAndAnonymize(
            Email.Create(CreateDeletedEmail(request.UserId)),
            PhoneNumber.Create(CreateDeletedPhoneNumber(request.UserId)));

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private static string CreateDeletedEmail(UserId userId) => $"deleted-user-{userId}@deleted.local";

    private static string CreateDeletedPhoneNumber(UserId userId)
    {
        var numericUserId = int.Parse(userId.ToString());
        var suffix = ((numericUserId % 1_000_000_000) + 100_000_000).ToString("D9");
        return $"+201{suffix}";
    }
}
