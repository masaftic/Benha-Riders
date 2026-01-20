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

        var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var role = Enum.Parse<RoleName>(request.Role, ignoreCase: true);
            user.AddRole(new UserRole(role));

            DriverProfile? driverProfile = null;
            RiderProfile? riderProfile = null;

            if (role == RoleName.Rider)
            {
                riderProfile = new RiderProfile(user.Id, user.Name);
                _db.RiderProfiles.Add(riderProfile);
            }
            else if (role == RoleName.Driver)
            {
                driverProfile = new DriverProfile(user.Id);
                _db.DriverProfiles.Add(driverProfile);
            }

            await _db.SaveChangesAsync(cancellationToken);

            var authResult = await _authenticationService.GenerateAuthenticatedResponseAsync(user, driverProfile, riderProfile, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return authResult;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Error.Failure("SELECT_ROLE_ERROR", "An error occurred while selecting the role. Please try again.");
        }
    }
}
