using System.ComponentModel;
using System.Data.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Features.Authentication.Services;
using BenhaScooters.Shared.Validation;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace BenhaScooters.Features.Authentication;


public record RegisterRequest(string Name, string Email, string PhoneNumber, string Password, string Role);

public record RegisterResponse(string Message, UserId UserId, bool RequiresPhoneVerification);

public class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("User name is required.");

        RuleFor(x => x.Email).NotEmpty()
            .Matches(ValidationRegex.Email).WithMessage("Valid email is required.");
        
        RuleFor(x => x.PhoneNumber).NotEmpty()
            .Matches(ValidationRegex.PhoneNumber).WithMessage("Valid phone number is required.");
        
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role.Equals("rider", StringComparison.CurrentCultureIgnoreCase)
                       || role.Equals("driver", StringComparison.CurrentCultureIgnoreCase))
            .WithMessage("Role must be either 'Rider' or 'Driver'.");
    }
}

public class Register : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly AppDbContext _db;

    public Register(IPasswordHasher passwordHasher, AppDbContext db)
    {
        _passwordHasher = passwordHasher;
        _db = db;
    }

    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Description(x => x
            .WithSummary("Register a new user")
            .Produces<RegisterResponse>()
            .Produces(StatusCodes.Status400BadRequest));
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var normalizedEmail = Domain.User.NormalizeEmail(Email.From(req.Email));
        if (await _db.Users.AnyAsync(x => x.EmailNormalized == normalizedEmail, ct))
        {
            ThrowError(req => req.Email, "Email already registered.", errorCode: "EmailAlreadyRegistered", statusCode: 400);
            return;
        }

        var normalizedPhone = Domain.User.NormalizePhone(PhoneNumber.From(req.PhoneNumber));
        if (await _db.Users.AnyAsync(x => x.PhoneNumberNormalized == normalizedPhone, ct))
        {
            ThrowError(p => p.PhoneNumber, "Phone number already registered.", errorCode: "PhoneNumberAlreadyRegistered", statusCode: 400);
            return;
        }

        var user = new User(
            req.Name,
            Email.From(req.Email),
            PhoneNumber.From(req.PhoneNumber),
            _passwordHasher.Hash(req.Password));

        var role = Enum.Parse<RoleName>(req.Role, ignoreCase: true);
        user.AddRole(new UserRole(role));

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var response = new RegisterResponse(
            "User registered successfully. Please verify your phone number before login.",
            user.Id,
            true);

        await SendAsync(response, cancellation: ct);
    }
}
