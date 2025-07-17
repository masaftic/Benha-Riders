using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Queries;

public record MeQuery(UserId UserId) : IRequest<ErrorOr<MeResponse>>;

public record MeResponse(
    UserId Id,
    string Name,
    Email Email,
    bool EmailVerified,
    PhoneNumber PhoneNumber,
    bool PhoneNumberVerified,
    DateTime CreatedAt,
    IEnumerable<RoleName> Roles);

public class MeQueryHandler : IRequestHandler<MeQuery, ErrorOr<MeResponse>>
{
    private readonly AppDbContext _db;

    public MeQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<MeResponse>> Handle(MeQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        var response = new MeResponse(
            user.Id,
            user.Name,
            user.Email,
            user.EmailVerified,
            user.PhoneNumber,
            user.PhoneNumberVerified,
            user.CreatedAt,
            user.Roles.Select(r => r.Name));

        return response;
    }
}
