using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record ChangePasswordCommand(UserId UserId, string CurrentPassword, string NewPassword, string ConfirmPassword) : IRequest<ErrorOr<ChangePasswordResponse>>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.NewPassword).WithMessage("Password confirmation must match new password.");
    }
}

public record ChangePasswordResponse(string Message);

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ErrorOr<ChangePasswordResponse>>
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ErrorOr<ChangePasswordResponse>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return UserErrors.IncorrectCurrentPassword;
        }

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));

        await _db.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResponse("Password changed successfully.");
    }
}
