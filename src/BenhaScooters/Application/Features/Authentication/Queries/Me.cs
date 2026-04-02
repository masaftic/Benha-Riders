using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
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
    IEnumerable<RoleName> Roles,
    DriverInfo? DriverInfo = null);

public class MeQueryHandler : IRequestHandler<MeQuery, ErrorOr<MeResponse>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public MeQueryHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<MeResponse>> Handle(MeQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.DriverProfile!)
                .ThenInclude(dp => dp.Documents)
            .Include(u => u.DriverProfile!)
                .ThenInclude(dp => dp.Vehicle)
            .Include(u => u.DriverProfile!)
                .ThenInclude(dp => dp.PersonalInfo)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return AppErrors.User.NotFound();
        }

        string? profileImage = null;
        var driverPhotoUrl = user.DriverProfile?.Documents.FirstOrDefault(d => d.Type == DocumentType.DriverPhoto)?.ImageUrl;
        if (driverPhotoUrl is not null)
        {
            profileImage = await _s3Service.GetPreSignedUrlAsync(driverPhotoUrl, TimeSpan.FromMinutes(60), cancellationToken);
        }

        // Update driver stats with new rating
        var driverRating = await _db.DriverStats
            .Where(ds => ds.UserId == user.Id)
            .Select(ds => ds.AverageRating)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        DriverInfo? driverInfo = null;
        if (user.DriverProfile?.Vehicle is not null && user.DriverProfile.PersonalInfo is not null && user.PhoneNumber is not null)
        {
            driverInfo = new DriverInfo(
                user.DriverProfile.PersonalInfo.FullName,
                user.PhoneNumber,
                profileImage,
                user.DriverProfile.Vehicle.Model,
                user.DriverProfile.Vehicle.Brand,
                user.DriverProfile.Vehicle.Color,
                user.DriverProfile.Vehicle.LicensePlate,
                driverRating);
        }

        var response = new MeResponse(
            user.Id,
            user.Name,
            user.Email,
            user.EmailVerified,
            user.PhoneNumber!,
            user.PhoneNumberVerified,
            user.CreatedAt,
            user.Roles.Select(r => r.Name),
            driverInfo);

        return response;
    }
}
