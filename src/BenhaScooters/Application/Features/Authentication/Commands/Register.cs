using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;


public record RegisterCommand(string Name, string Email, string PhoneNumber, string Password) : IRequest<ErrorOr<OnboardingStatusToken>>;


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
    }
}


public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<OnboardingStatusToken>>
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(IPasswordHasher passwordHasher, AppDbContext db, IJwtService jwtService)
    {
        _passwordHasher = passwordHasher;
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<ErrorOr<OnboardingStatusToken>> Handle(RegisterCommand req, CancellationToken ct)
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

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var nextStep = UserOnboardingStateMachine.GetNextStep(user.Status);
        var token = _jwtService.GenerateOnboardingToken(user.Id, user.Status, nextStep);
        return new OnboardingStatusToken(token, nextStep);
    }
}