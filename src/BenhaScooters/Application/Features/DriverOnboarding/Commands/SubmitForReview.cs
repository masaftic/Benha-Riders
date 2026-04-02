using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record SubmitForReviewCommand(UserId DriverId) : IRequest<ErrorOr<Success>>;

public class SubmitForReviewCommandHandler(AppDbContext db) : IRequestHandler<SubmitForReviewCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;

        var driver = await db.DriverProfiles
            .Where(d => d.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (driver is null)
            return AppErrors.Driver.NotFound();
        
        var result = driver.SubmitForReview();
        if (result.IsError)
            return result.Errors;

        await db.SaveChangesAsync();
        return result;
    }
}
