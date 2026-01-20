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

// Rider (semantic wrapper around UserId)
[EfCoreConverter<RiderId>]

// Driver (semantic wrapper around UserId) and value objects
[EfCoreConverter<DriverId>]
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

public partial class VogenEfCoreConverters;
