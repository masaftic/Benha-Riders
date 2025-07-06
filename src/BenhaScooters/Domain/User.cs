using System.Text.RegularExpressions;
using Vogen;

namespace BenhaScooters.Domain;

[ValueObject<Guid>]
public partial struct UserId;


[ValueObject<string>]
public partial struct Email
{
    private static Validation Validate(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Validation.Invalid("Email cannot be empty.");

        if (Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") == false)
            return Validation.Invalid("Invalid email format.");

        return Validation.Ok;
    }
}


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
    public PhoneNumber PhoneNumber { get; private set; }
    public PhoneNumber PhoneNumberNormalized { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    private List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private User() { }

    public User(UserId id, string name, Email email, PhoneNumber phoneNumber, string passwordHash)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        Id = id;
        Name = name;
        Email = email;
        EmailNormalized = Email.From(email.Value.ToLowerInvariant());
        PhoneNumber = phoneNumber;
        PhoneNumberNormalized = NormalizePhone(phoneNumber);
        PasswordHash = passwordHash;
    }

    private static PhoneNumber NormalizePhone(PhoneNumber phone)
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
}


