using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record ApproveDriverCommand(UserId DriverId) : IRequest<ErrorOr<Success>>;

public class ApproveDriverCommandValidator : AbstractValidator<ApproveDriverCommand>
{
    public ApproveDriverCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty().WithMessage("Driver user ID is required.");
    }
}

public class ApproveDriverCommandHandler : IRequestHandler<ApproveDriverCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public ApproveDriverCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(ApproveDriverCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;

        var driverProfile = await _db.DriverProfiles
            .Include(dp => dp.Documents)
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AdminErrors.DriverNotFound;
        }

        // Admin approves the driver profile
        var adminUserId = UserId.From(1); // TODO: Get from current user context
        var approveResult = driverProfile.Approve(adminUserId);
        if (approveResult.IsError)
        {
            return approveResult.Errors;
        }

        if (!await _db.DriverWallets.AnyAsync(dw => dw.DriverUserId == userId, cancellationToken))
        {
            var wallet = new DriverWallet(userId);
            _db.DriverWallets.Add(wallet);
        }

        if (!await _db.DriverStatuses.AnyAsync(ds => ds.UserId == userId, cancellationToken))
        {
            var driverStatus = new DriverStatus(userId);
            _db.DriverStatuses.Add(driverStatus);
        }

        if (!await _db.DriverLocations.AnyAsync(dl => dl.UserId == userId, cancellationToken))
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var location = geometryFactory.CreatePoint(new Coordinate(0, 0));
            var driverLocation = new DriverLocation(userId, location);
            _db.DriverLocations.Add(driverLocation);
        }

        if (!await _db.DriverStats.AnyAsync(ds => ds.UserId == userId, cancellationToken))
        {
            var driverStats = new DriverStats(userId);
            _db.DriverStats.Add(driverStats);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
