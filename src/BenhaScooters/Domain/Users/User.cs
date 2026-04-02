using System.Text.RegularExpressions;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using Thinktecture;

namespace BenhaScooters.Domain.Users;

[ValueObject<int>]
public partial struct UserId;


[ValueObject<string>]
[KeyMemberEqualityComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
[KeyMemberComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
public partial class Email
{
    static partial void ValidateFactoryArguments(
        ref ValidationError? validationError,
        ref string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationError = new ValidationError("Email cannot be empty.");
            return;
        }

        if (!Regex.IsMatch(value, ValidationRegex.Email))
        {
            validationError = new ValidationError("Invalid email format.");
            return;
        }

        value = value.Trim().ToLowerInvariant();
    }
}


[ValueObject<string>]
[KeyMemberEqualityComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
[KeyMemberComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
public partial class PhoneNumber
{
    static partial void ValidateFactoryArguments(
        ref ValidationError? validationError,
        ref string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationError = new ValidationError("رقم الهاتف مطلوب.");
            return;
        }

        if (!EgyptianPhoneNumberRegex().IsMatch(value))
        {
            validationError = new ValidationError("رقم الهاتف غير صالح. يجب أن يكون رقمًا مصريًا.");
            return;
        }

        string normalizedPhone = value.Trim().Replace(" ", "").Replace("-", "");
        if (normalizedPhone.StartsWith('0'))
        {
            normalizedPhone = "+20" + normalizedPhone[1..];
        }
        else if (!normalizedPhone.StartsWith("+20"))
        {
            normalizedPhone = "+20" + normalizedPhone;
        }

        value = normalizedPhone;
    }

    [GeneratedRegex(ValidationRegex.PhoneNumber)]
    private static partial Regex EgyptianPhoneNumberRegex();
}


public class User
{
    public UserId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public bool EmailVerified { get; private set; } = false;
    public PhoneNumber? PhoneNumber { get; private set; }
    public bool PhoneNumberVerified { get; private set; } = false;
    public string? PasswordHash { get; private set; } = null;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<UserRole> _roles = [];
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<ExternalAuth> _externalAuths = [];
    public IReadOnlyCollection<ExternalAuth> ExternalAuths => _externalAuths.AsReadOnly();


    public DriverProfile? DriverProfile { get; private set; }
    public RiderProfile? RiderProfile { get; private set; }


    private User() { }

    public User(string name, Email email, PhoneNumber? phoneNumber, string? passwordHash)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
    }


    public ErrorOr<Success> AddRole(UserRole role)
    {
        if (_roles.Any(r => r.Name == role.Name))
            return AppErrors.User.AlreadyHasRole();

        _roles.Add(role);
        return Result.Success;
    }

    public void RemoveRole(UserRole role)
    {
        if (!_roles.Remove(role))
            throw new InvalidOperationException($"User does not have the role {role.Name}.");
    }

    public bool HasRole(RoleName roleName)
    {
        return _roles.Any(r => r.Name == roleName);
    }

    public RefreshToken CreateRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token);
        refreshToken?.Revoke();
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(rt => rt.IsActive))
        {
            token.Revoke();
        }
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
    }

    public void VerifyPhoneNumber(PhoneNumber? number = null)
    {
        if (PhoneNumber == null && number != null)
        {
            PhoneNumber = number;
        }

        PhoneNumberVerified = true;
    }


    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrEmpty(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;

        // Revoke all refresh tokens to force re-login
        RevokeAllRefreshTokens();
    }

    public void AddExternalAuth(ExternalAuth externalAuth)
    {
        if (_externalAuths.Any(ea => ea.Provider == externalAuth.Provider && ea.ProviderUserId == externalAuth.ProviderUserId))
            throw new InvalidOperationException("External auth already exists for this provider and user ID.");

        _externalAuths.Add(externalAuth);
    }

    public void CreateDriverProfile()
    {
        if (DriverProfile != null)
            throw new InvalidOperationException("User already has a driver profile.");

        DriverProfile = new DriverProfile(this);
    }

    public void CreateRiderProfile()
    {
        if (RiderProfile != null)
            throw new InvalidOperationException("User already has a rider profile.");

        RiderProfile = new RiderProfile(this, Name);
    }
}
