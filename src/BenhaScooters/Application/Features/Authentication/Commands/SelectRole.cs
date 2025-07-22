using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record SelectRoleCommand(UserId UserId, string Role) : IRequest<ErrorOr<AuthenticatedResponse>>;

public class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
{
    public SelectRoleCommandValidator()
    {
        RuleFor(x => x.UserId.Value)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role.Equals("rider", StringComparison.CurrentCultureIgnoreCase)
                       || role.Equals("driver", StringComparison.CurrentCultureIgnoreCase))
            .WithMessage("Role must be either 'Rider' or 'Driver'.");
    }
}

public class SelectRoleCommandHandler : IRequestHandler<SelectRoleCommand, ErrorOr<AuthenticatedResponse>>
{
    private readonly AppDbContext _db;
    private readonly IAuthenticationService _authenticationService;

    public SelectRoleCommandHandler(AppDbContext db, IAuthenticationService authenticationService)
    {
        _db = db;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<AuthenticatedResponse>> Handle(SelectRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return UserErrors.UserNotFound;
        }

        if (user.Roles.Any())
        {
            return UserErrors.RoleAlreadyAssigned;
        }

        // Use state machine to validate user can perform this step
        if (!UserOnboardingStateMachine.CanPerformStep(user.Status, OnboardingSteps.SelectRole))
        {
            return UserErrors.PhoneNumberNotVerified;
        }
        
        var role = Enum.Parse<RoleName>(request.Role, ignoreCase: true);
        user.AddRole(new UserRole(role));

        Driver? driver = null;
        Rider? rider = null;

        if (role == RoleName.Rider)
        {
            rider = new Rider(user.Id, user.Name);
            _db.Riders.Add(rider);
        }
        else if (role == RoleName.Driver)
        {
            driver = new Driver(user.Id);
            _db.Drivers.Add(driver);
        }

        // Use state machine to get new status after completing role selection
        var newStatusResult = UserOnboardingStateMachine.GetNewStatusAfterStep(user.Status, OnboardingSteps.SelectRole);
        if (newStatusResult.IsError) return newStatusResult.Errors;

        var result = user.UpdateStatus(newStatusResult.Value);
        if (result.IsError) return result.Errors;

        return await _authenticationService.GenerateAuthenticatedResponseAsync(user, driver, rider, cancellationToken);
    }
}
