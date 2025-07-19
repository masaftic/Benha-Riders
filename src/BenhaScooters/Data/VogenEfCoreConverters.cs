using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using Vogen;

namespace BenhaScooters.Data;

// User
[EfCoreConverter<UserId>]
[EfCoreConverter<Email>]
[EfCoreConverter<UserRoleId>]
[EfCoreConverter<PhoneNumber>]

// Refresh token
[EfCoreConverter<RefreshTokenId>]

// Sms verification code
[EfCoreConverter<SmsVerificationCodeId>]

// Rider
[EfCoreConverter<RiderId>]

// Driver
[EfCoreConverter<NationalId>]
[EfCoreConverter<LicensePlate>]
[EfCoreConverter<DriverId>]
[EfCoreConverter<DriverRatingId>]
[EfCoreConverter<DriverLocationId>]
[EfCoreConverter<DriverAvailabilityId>]
[EfCoreConverter<DriverMatchAttemptId>]

[EfCoreConverter<TripId>]
[EfCoreConverter<TripRequestId>]
[EfCoreConverter<TripGpsPointId>]
[EfCoreConverter<TripRatingId>]
[EfCoreConverter<TripFareId>]
[EfCoreConverter<TripRouteId>]
public partial class VogenEfCoreConverters;
