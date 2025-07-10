using BenhaScooters.Domain;
using BenhaScooters.Domain.Driver;
using BenhaScooters.Domain.Driver.ValueObjects;
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

// Driver
[EfCoreConverter<NationalId>]
[EfCoreConverter<DriverProfileId>]
[EfCoreConverter<LicensePlate>]
public partial class VogenEfCoreConverters;
