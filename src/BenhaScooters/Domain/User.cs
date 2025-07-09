using System.Text.RegularExpressions;
using Vogen;

namespace BenhaScooters.Domain;

[ValueObject<Guid>]
public partial struct UserId;


[ValueObject<string>]
public partial struct Email;


[ValueObject<string>]
public partial struct PhoneNumber
{
    private static Validation Validate(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Validation.Invalid("Phone number cannot be empty.");

        // Basic validation for Egyptian phone numbers
        if (!Regex.IsMatch(phoneNumber, @"^(\+20|0)?[1-9][0-9]{9}$"))
            return Validation.Invalid("Invalid phone number format.");

        return Validation.Ok;
    }
}


public class User
{
    public UserId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Email Email { get; private set; }
    public Email EmailNormalized { get; private set; }
    public bool EmailVerified { get; private set; } = false;
    public PhoneNumber PhoneNumber { get; private set; }
    public PhoneNumber PhoneNumberNormalized { get; private set; }
    public bool PhoneNumberVerified { get; private set; } = false;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    // public DriverProfileId? DriverProfileId { get; private set; }
    // public DriverProfile? DriverProfile { get; private set; } = null!;
    private List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    
    private List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public User(string name, Email email, PhoneNumber phoneNumber, string passwordHash)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        Id = UserId.From(Guid.NewGuid());
        Name = name;
        Email = email;
        EmailNormalized = NormalizeEmail(email);
        PhoneNumber = phoneNumber;
        PhoneNumberNormalized = NormalizePhone(phoneNumber);
        PasswordHash = passwordHash;
    }

    public static PhoneNumber NormalizePhone(PhoneNumber phone)
    {
        var normalizedPhone = phone.Value.Trim().Replace(" ", "").Replace("-", "");
        if (normalizedPhone.StartsWith("0"))
        {
            normalizedPhone = "+20" + normalizedPhone.Substring(1);
        }
        else if (!normalizedPhone.StartsWith("+20"))
        {
            normalizedPhone = "+20" + normalizedPhone;
        }
        return PhoneNumber.From(normalizedPhone);
    }

    public static Email NormalizeEmail(Email email)
    {
        return Email.From(email.Value.ToLowerInvariant().Trim());
    }

    public void AddRole(UserRole role)
    {
        if (_roles.Any(r => r.Name == role.Name))
            throw new InvalidOperationException($"User already has the role {role.Name}.");

        _roles.Add(role);
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

    public void VerifyPhoneNumber()
    {
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
}


