using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Services;
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


public record RegisterCommand(string Name, Email Email, PhoneNumber PhoneNumber, string Password, App App) : IRequest<ErrorOr<AuthenticationResponse>>;


public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم المستخدم مطلوب.");
        
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");
    }
}


public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<AuthenticationResponse>>
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly AppDbContext _db;
    private readonly IAuthenticationService _authenticationService;

    public RegisterCommandHandler(IPasswordHasher passwordHasher, AppDbContext db, IAuthenticationService authenticationService)
    {
        _passwordHasher = passwordHasher;
        _db = db;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Handle(RegisterCommand req, CancellationToken ct)
    {
        var normalizedEmail = req.Email;
        if (await _db.Users.AnyAsync(x => x.Email == normalizedEmail, ct))
        {
            return AppErrors.User.AlreadyExists(req.Email);
        }

        var normalizedPhone = req.PhoneNumber;
        if (await _db.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone, ct))
        {
            return AppErrors.User.PhoneAlreadyExists(req.PhoneNumber);
        }

        var user = new User(
            req.Name,
            req.Email,
            req.PhoneNumber,
            _passwordHasher.Hash(req.Password));

        _db.Users.Add(user);

        if (req.App == App.DriverApp)
        {
            user.CreateDriverProfile();
        }
        else if (req.App == App.RiderApp)
        {
            user.CreateRiderProfile();
        }

        await _db.SaveChangesAsync(ct);

        // User hasn't verified phone yet, return onboarding required
        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, req.App, user.DriverProfile, user.RiderProfile, ct);
        return new AuthenticationResponse("onboarding_required", new OnboardingRequired(authenticatedResponse.AccessToken, "verify_phone"));
    }
}
