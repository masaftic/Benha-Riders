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
            .NotEmpty().WithMessage("كلمة المرور الحالية مطلوبة.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(6).WithMessage("يجب أن تتكون كلمة المرور الجديدة من 6 أحرف على الأقل.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور مطلوب.")
            .Equal(x => x.NewPassword).WithMessage("يجب أن يتطابق تأكيد كلمة المرور مع كلمة المرور الجديدة.");
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
            return AppErrors.User.NotFound();
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return AppErrors.User.IncorrectCurrentPassword();
        }

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));

        await _db.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResponse("Password changed successfully.");
    }
}
