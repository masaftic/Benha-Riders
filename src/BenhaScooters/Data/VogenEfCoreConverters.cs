using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using Vogen;

namespace BenhaScooters.Data;

// User & Auth
[EfCoreConverter<UserId>]
[EfCoreConverter<Email>]
[EfCoreConverter<UserRoleId>]
[EfCoreConverter<PhoneNumber>]
[EfCoreConverter<RefreshTokenId>]
[EfCoreConverter<SmsVerificationCodeId>]


[EfCoreConverter<NationalId>]
[EfCoreConverter<LicensePlate>]
[EfCoreConverter<VIN>]

// Trip
[EfCoreConverter<TripRequestId>]
[EfCoreConverter<TripId>]
[EfCoreConverter<TripGpsPointId>]
[EfCoreConverter<TripRatingId>]

// Matching
[EfCoreConverter<DriverMatchAttemptId>]
[EfCoreConverter<MatchingSessionId>]

// Wallet
[EfCoreConverter<DriverWalletId>]
[EfCoreConverter<WalletTransactionId>]

public partial class VogenEfCoreConverters;
