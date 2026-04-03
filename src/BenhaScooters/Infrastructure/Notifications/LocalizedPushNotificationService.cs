using System.Globalization;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Notifications.Localization;
using BenhaScooters.Shared.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace BenhaScooters.Infrastructure.Notifications;

public sealed class LocalizedPushNotificationService : ILocalizedPushNotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IStringLocalizer<FcmNotificationResources> _localizer;

    public LocalizedPushNotificationService(
        AppDbContext dbContext,
        IPushNotificationService pushNotificationService,
        IStringLocalizer<FcmNotificationResources> localizer)
    {
        _dbContext = dbContext;
        _pushNotificationService = pushNotificationService;
        _localizer = localizer;
    }

    public async Task NotifyDriverApprovedAsync(UserId driverId, CancellationToken cancellationToken = default)
    {
        await SendAsync(
            driverId,
            "DriverApproved_Title",
            "DriverApproved_Body",
            new Dictionary<string, string>
            {
                ["type"] = "driver_approved"
            },
            cancellationToken);
    }

    public async Task NotifyTripAssignedToRiderAsync(
        UserId riderId,
        string tripId,
        string driverName,
        string vehicleLicensePlate,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            riderId,
            "TripAssigned_Title",
            "TripAssigned_Body",
            new Dictionary<string, string>
            {
                ["type"] = "trip_assigned",
                ["tripId"] = tripId,
                ["driverName"] = driverName,
                ["vehicleLicensePlate"] = vehicleLicensePlate,
            },
            cancellationToken,
            driverName,
            vehicleLicensePlate);
    }

    public async Task NotifyTripRequestCanceledAsync(
        UserId riderId,
        string tripRequestId,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            riderId,
            "TripRequestCanceled_Title",
            "TripRequestCanceled_Body",
            new Dictionary<string, string>
            {
                ["type"] = "trip_request_canceled",
                ["tripRequestId"] = tripRequestId,
            },
            cancellationToken);
    }

    public async Task NotifyRideRequestOfferToDriverAsync(
        UserId driverId,
        string matchAttemptId,
        string riderName,
        decimal estimatedFare,
        string pickupLocation,
        string dropoffLocation,
        string? pickupAddress,
        string? dropoffAddress,
        double distanceToPickupKm,
        double estimatedArrivalMinutes,
        CancellationToken cancellationToken = default)
    {
        var language = await GetLanguageAsync(driverId, cancellationToken);
        using var _ = UseCulture(language);

        var formattedFare = estimatedFare.ToString("F0", CultureInfo.CurrentCulture);
        var title = _localizer["RideRequestOffer_Title"].Value;
        var body = _localizer["RideRequestOffer_Body", riderName, formattedFare].Value;

        await _pushNotificationService.SendToUserAsync(
            driverId,
            title,
            body,
            new Dictionary<string, string>
            {
                ["type"] = "ride_request_offer",
                ["matchAttemptId"] = matchAttemptId,
                ["riderName"] = riderName,
                ["pickupLocation"] = pickupLocation,
                ["dropoffLocation"] = dropoffLocation,
                ["pickupAddress"] = pickupAddress ?? _localizer["Common_UnknownPickupLocation"].Value,
                ["dropoffAddress"] = dropoffAddress ?? _localizer["Common_UnknownDropoffLocation"].Value,
                ["fare"] = estimatedFare.ToString(CultureInfo.InvariantCulture),
                ["distanceToPickup"] = distanceToPickupKm.ToString(CultureInfo.InvariantCulture),
                ["estimatedArrival"] = estimatedArrivalMinutes.ToString(CultureInfo.InvariantCulture),
            },
            cancellationToken);
    }

    public async Task NotifyDriverArrivedToRiderAsync(
        UserId riderId,
        string tripId,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            riderId,
            "DriverArrived_Title",
            "DriverArrived_Body",
            new Dictionary<string, string>
            {
                ["type"] = "driver_arrived",
                ["tripId"] = tripId
            },
            cancellationToken);
    }

    public async Task NotifyTripCancelledToRiderAsync(
        UserId riderId,
        string tripId,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            riderId,
            "TripCancelledToRider_Title",
            "TripCancelledToRider_Body",
            new Dictionary<string, string>
            {
                ["type"] = "trip_cancelled",
                ["tripId"] = tripId
            },
            cancellationToken);
    }

    public async Task NotifyTripCancelledToDriverAsync(
        UserId driverId,
        string tripId,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            driverId,
            "TripCancelledToDriver_Title",
            "TripCancelledToDriver_Body",
            new Dictionary<string, string>
            {
                ["type"] = "trip_cancelled",
                ["tripId"] = tripId
            },
            cancellationToken);
    }

    private async Task SendAsync(
        UserId userId,
        string titleKey,
        string bodyKey,
        Dictionary<string, string> data,
        CancellationToken cancellationToken,
        params object[] formatArguments)
    {
        var language = await GetLanguageAsync(userId, cancellationToken);
        using var _ = UseCulture(language);

        var title = _localizer[titleKey].Value;
        var body = formatArguments.Length == 0
            ? _localizer[bodyKey].Value
            : _localizer[bodyKey, formatArguments].Value;

        await _pushNotificationService.SendToUserAsync(userId, title, body, data, cancellationToken);
    }

    private async Task<string> GetLanguageAsync(UserId userId, CancellationToken cancellationToken)
    {
        var preferredLanguage = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.PreferredLanguage)
            .FirstOrDefaultAsync(cancellationToken);

        return AppLanguages.Normalize(preferredLanguage) ?? AppLanguages.Arabic;
    }

    private static IDisposable UseCulture(string language)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.GetCultureInfo(language);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        return new CultureScope(previousCulture, previousUiCulture);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        public CultureScope(CultureInfo previousCulture, CultureInfo previousUiCulture)
        {
            _previousCulture = previousCulture;
            _previousUiCulture = previousUiCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
