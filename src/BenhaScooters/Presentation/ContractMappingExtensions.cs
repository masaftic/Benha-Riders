using AuthContracts = BenhaScooters.Contracts.Authentication;
using DriverContracts = BenhaScooters.Contracts.DriverOnboarding;
using WalletContracts = BenhaScooters.Contracts.Wallet;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Presentation;

internal static class ContractMappingExtensions
{
    public static App ToDomain(this AuthContracts.App app) => app switch
    {
        AuthContracts.App.DriverApp => App.DriverApp,
        AuthContracts.App.RiderApp => App.RiderApp,
        _ => throw new ArgumentOutOfRangeException(nameof(app), app, "Unknown app")
    };

    public static Domain.Drivers.Enums.VehicleType ToDomain(this DriverContracts.VehicleType vehicleType) =>
        (Domain.Drivers.Enums.VehicleType)(int)vehicleType;

    public static Domain.Drivers.DriverOnboardingStatus ToDomain(this DriverContracts.DriverOnboardingStatus status) =>
        (Domain.Drivers.DriverOnboardingStatus)(int)status;

    public static Domain.Drivers.WalletTransactionType ToDomain(this WalletContracts.WalletTransactionType type) =>
        (Domain.Drivers.WalletTransactionType)(int)type;

    public static Domain.Drivers.TopUpRequestStatus ToDomain(this WalletContracts.TopUpRequestStatus status) =>
        (Domain.Drivers.TopUpRequestStatus)(int)status;

    public static Coordinate ToCoordinate(this double latitude, double longitude) =>
        Coordinate.Create(Latitude.Create(latitude), Longitude.Create(longitude));

    public static DriverMatchAttemptId ToDriverMatchAttemptId(this int id) =>
        DriverMatchAttemptId.Create(id);

    public static TripRequestId ToTripRequestId(this int id) =>
        TripRequestId.Create(id);
}
