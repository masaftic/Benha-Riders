using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record LogoutCommand(UserId UserId, string? RefreshToken = null) : IRequest<ErrorOr<LogoutResponse>>;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        // RefreshToken is optional - if not provided, we'll revoke all tokens for the user
    }
}

public record LogoutResponse(string Message);

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ErrorOr<LogoutResponse>>
{
    private readonly AppDbContext _db;

    public LogoutCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<LogoutResponse>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // Revoke specific refresh token
            user.RevokeRefreshToken(request.RefreshToken);
        }
        else
        {
            // Revoke all refresh tokens for the user
            user.RevokeAllRefreshTokens();
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new LogoutResponse("Logged out successfully.");
    }
}
