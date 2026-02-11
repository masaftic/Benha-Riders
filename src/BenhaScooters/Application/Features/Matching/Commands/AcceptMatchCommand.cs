using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Common.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Infrastructure.S3;
using DriverInfoDto = BenhaScooters.Application.Features.Trips.Queries.Common.DriverInfo;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Notifications;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Matching.Commands;

public record AcceptMatchCommand(
    UserId DriverId,
    DriverMatchAttemptId DriverMatchAttemptId) : IRequest<ErrorOr<AcceptMatchResult>>;

public record AcceptMatchResult(TripId TripId, DateTime AcceptedAt);

public class AcceptMatchCommandValidator : AbstractValidator<AcceptMatchCommand>
{
    public AcceptMatchCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.DriverMatchAttemptId)
            .NotEmpty()
            .WithMessage("Driver Match Attempt ID is required");
    }
}

public class AcceptMatchCommandHandler(
    AppDbContext db, 
    IPublisher publisher, 
    IOptions<DriverWalletOptions> walletOptions,
    IHubContext<RiderHub, IRiderNotifications> riderHub,
    IS3Service s3Service,
    ILogger<AcceptMatchCommandHandler> logger) : IRequestHandler<AcceptMatchCommand, ErrorOr<AcceptMatchResult>>
{
    public async Task<ErrorOr<AcceptMatchResult>> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // Check wallet debt limit before allowing match acceptance
        var debtLimit = (decimal)walletOptions.Value.DebtLimitEgp;
        var wallet = await db.DriverWallets
            .FirstOrDefaultAsync(w => w.DriverUserId == request.DriverId, cancellationToken);

        if (wallet is null)
        {
            logger.LogWarning("Wallet not found for driver {DriverId} when accepting match", request.DriverId);
            return WalletErrors.NotFound;
        }

        if (!wallet.CanAcceptMatch(debtLimit))
        {
            logger.LogWarning("Driver {DriverId} cannot accept match due to debt limit exceeded. Balance: {Balance}, Limit: {Limit}",
                request.DriverId, wallet.Balance, debtLimit);
            return WalletErrors.DebtLimitExceeded;
        }

        // Locate the driver match attempt being accepted
        var matchAttempt = await db.DriverMatchAttempts
            .Include(ma => ma.MatchingSession)
            .ThenInclude(ms => ms.TripRequest)
            .FirstOrDefaultAsync(ma => ma.Id == request.DriverMatchAttemptId, cancellationToken);

        if (matchAttempt is null || matchAttempt.DriverUserId != request.DriverId)
        {
            logger.LogWarning("Match attempt {MatchAttemptId} not found or does not belong to driver {DriverId}",
                request.DriverMatchAttemptId, request.DriverId);
            return MatchingErrors.MatchAttempt.NotFound;
        }

        if (matchAttempt.ExpiresAt <= now)
        {
            logger.LogWarning("Match attempt {MatchAttemptId} for driver {DriverId} is expired",
                request.DriverMatchAttemptId, request.DriverId);
            return MatchingErrors.MatchAttempt.Expired;
        }

        if (matchAttempt.Status != MatchAttemptStatus.Pending)
        {
            logger.LogWarning("Match attempt {MatchAttemptId} for driver {DriverId} is not pending (status: {Status})",
                request.DriverMatchAttemptId, request.DriverId, matchAttempt.Status);
            return MatchingErrors.MatchAttempt.NotFound;
        }

        var matchingSession = matchAttempt.MatchingSession;

        if (matchingSession is null)
        {
            logger.LogWarning("No matching session found for match attempt {MatchAttemptId}",
                request.DriverMatchAttemptId);
            return MatchingErrors.Session.NotFound;
        }

        var acceptResult = matchingSession.AcceptMatch(request.DriverId);
        if (acceptResult.IsError)
        {
            logger.LogWarning("Failed to accept match for driver {DriverId}: {Errors}",
                request.DriverId, string.Join(", ", acceptResult.Errors.Select(e => e.Description)));
            return acceptResult.Errors;
        }

        var userId = request.DriverId;
        var driverStatus = await db.DriverStatuses
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverStatus is null)
        {
            logger.LogWarning("Driver status not found for driver {DriverId} when accepting match", request.DriverId);
            return DriverErrors.DriverNotFound;
        }

        var tripRequest = matchingSession.TripRequest;

        var trip = new Trip(
            driverStatus.UserId,
            tripRequest.RiderId,
            tripRequest.PickupLocation,
            tripRequest.DropoffLocation,
            tripRequest.PickupAddress,
            tripRequest.DropoffAddress,
            tripRequest.FinalFare);

        db.Trips.Add(trip);

        // Save to generate TripId
        await db.SaveChangesAsync(cancellationToken);

        var startTripResult = driverStatus.StartTrip(trip.Id);
        if (startTripResult.IsError)
        {
            logger.LogWarning("Failed to start trip {TripId} for driver {DriverId}: {Errors}",
                trip.Id,
                request.DriverId,
                string.Join(", ", startTripResult.Errors.Select(e => e.Description)));
            await transaction.RollbackAsync(cancellationToken);
            return startTripResult.Errors;
        }

        var tripRoute = new TripRoute(trip.Id);
        db.TripRoutes.Add(tripRoute);

        var markMatchedResult = tripRequest.MarkAsMatched(driverStatus.UserId);
        if (markMatchedResult.IsError)
        {
            logger.LogWarning("Failed to mark trip request {TripRequestId} as matched: {Errors}",
                tripRequest.Id,
                string.Join(", ", markMatchedResult.Errors.Select(e => e.Description)));
            await transaction.RollbackAsync(cancellationToken);
            return markMatchedResult.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        // Publish trip created event for downstream listeners
        await publisher.Publish(trip.CreateTripCreatedEvent(), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        // Notify rider via SignalR about trip assignment
        var driverProfile = await db.DriverProfiles
            .Include(dp => dp.User)
            .Include(dp => dp.Vehicle)
            .Include(dp => dp.Documents)
            .FirstOrDefaultAsync(dp => dp.UserId == driverStatus.UserId, cancellationToken);

        if (driverProfile != null)
        {
            var driverPhotoUrl = driverProfile.Documents.FirstOrDefault(d => d.Type == DocumentType.DriverPhoto)?.ImageUrl;
            var fullDriverPhotoUrl = await s3Service.GetPreSignedUrlAsync(driverPhotoUrl!, TimeSpan.FromHours(1), cancellationToken);
            
            var driverInfo = new DriverInfoDto(
                driverProfile.PersonalInfo?.FullName ?? "Driver",
                driverProfile.User.PhoneNumber!,
                fullDriverPhotoUrl,
                driverProfile.Vehicle?.Model ?? "Unknown",
                driverProfile.Vehicle?.Brand ?? "Unknown",
                driverProfile.Vehicle?.Color ?? "Unknown",
                driverProfile.Vehicle?.LicensePlate ?? LicensePlate.Create("UNKNOWN"));

            var notification = new TripAssignedNotification(
                TripId: trip.Id,
                Driver: driverInfo,
                EstimatedArrivalMinutes: matchAttempt.EstimatedArrivalTime / 60.0, // Convert seconds to minutes
                AssignedAt: trip.AssignedAt);

            await riderHub.Clients.Group(tripRequest.RiderId.ToString())
                .NotifyTripAssigned(tripRequest.RiderId.ToString(), notification);

            logger.LogInformation("Notified rider {RiderId} about trip assignment {TripId}",
                tripRequest.RiderId, trip.Id);
        }

        return new AcceptMatchResult(trip.Id, trip.AssignedAt);
    }
}
