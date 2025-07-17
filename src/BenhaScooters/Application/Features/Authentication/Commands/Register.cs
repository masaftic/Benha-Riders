using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;


public record RegisterCommand(string Name, string Email, string PhoneNumber, string Password, string Role) : IRequest<ErrorOr<RegisterResponse>>;


public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
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


public record RegisterResponse(string Message, UserId UserId, bool RequiresPhoneVerification);


public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<RegisterResponse>>
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly AppDbContext _db;

    public RegisterCommandHandler(IPasswordHasher passwordHasher, AppDbContext db)
    {
        _passwordHasher = passwordHasher;
        _db = db;
    }

    public async Task<ErrorOr<RegisterResponse>> Handle(RegisterCommand req, CancellationToken ct)
    {
        var normalizedEmail = User.NormalizeEmail(Email.From(req.Email));
        if (await _db.Users.AnyAsync(x => x.EmailNormalized == normalizedEmail, ct))
        {
            return UserErrors.EmailAlreadyExists;
        }

        var normalizedPhone = User.NormalizePhone(PhoneNumber.From(req.PhoneNumber));
        if (await _db.Users.AnyAsync(x => x.PhoneNumberNormalized == normalizedPhone, ct))
        {
            return UserErrors.PhoneAlreadyExists;
        }

        var user = new User(
            req.Name,
            Email.From(req.Email),
            PhoneNumber.From(req.PhoneNumber),
            _passwordHasher.Hash(req.Password));

        var role = Enum.Parse<RoleName>(req.Role, ignoreCase: true);
        user.AddRole(new UserRole(role));

        _db.Users.Add(user);

        // Save to get the user ID
        // TODO: maybe a transaction here
        await _db.SaveChangesAsync(ct);

        if (role == RoleName.Rider)
        {
            _db.Riders.Add(new Rider(user.Id, req.Name));
        }
        else if (role == RoleName.Driver)
        {
            _db.Drivers.Add(new Driver(user.Id));
        }

        await _db.SaveChangesAsync(ct);

        return new RegisterResponse("User registered successfully. Please verify your phone number before login.", user.Id, true);
    }
}