using BenhaScooters.Domain;
using Vogen;

namespace BenhaScooters.Data;


[EfCoreConverter<UserId>]
[EfCoreConverter<Email>]
[EfCoreConverter<UserRoleId>]
[EfCoreConverter<PhoneNumber>]

[EfCoreConverter<RefreshTokenId>]
public partial class VogenEfCoreConverters;
