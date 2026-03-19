using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record RegisterDeviceTokenCommand(UserId UserId, string Token, string Platform) : IRequest<ErrorOr<Success>>;

public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _dbContext;

    public RegisterDeviceTokenCommandHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ErrorOr<Success>> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var platform = request.Platform.ToLowerInvariant();
        if (platform is not ("android" or "ios"))
            return Error.Validation("INVALID_PLATFORM", "Platform must be 'android' or 'ios'.");

        // Check if user already has a token for this platform
        var existing = await _dbContext.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == request.UserId && t.Platform == platform, cancellationToken);

        if (existing != null)
        {
            existing.UpdateToken(request.Token);
        }
        else
        {
            // Remove this token from any other user (token transferred)
            var oldTokens = await _dbContext.UserDeviceTokens
                .Where(t => t.Token == request.Token)
                .ToListAsync(cancellationToken);
            _dbContext.UserDeviceTokens.RemoveRange(oldTokens);

            _dbContext.UserDeviceTokens.Add(new UserDeviceToken(request.UserId, request.Token, platform));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
