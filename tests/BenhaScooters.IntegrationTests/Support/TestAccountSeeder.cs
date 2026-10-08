using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.IntegrationTests.Support;

public sealed record TestRider(User User, RiderProfile Profile, string AccessToken);

public sealed record TestDriver(
    User User,
    DriverProfile Profile,
    DriverStatus Status,
    DriverLocation Location,
    DriverWallet Wallet,
    string AccessToken);

public sealed class TestAccountSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly ISender _sender;

    public TestAccountSeeder(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        ISender sender)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _sender = sender;
    }

    public async Task<TestRider> CreateVerifiedRiderAsync(
        string name = "Test Rider",
        string email = "rider@test.local",
        string phoneNumber = "+201000000001",
        CancellationToken cancellationToken = default)
    {
        var user = new User(
            name,
            Email.Create(email),
            PhoneNumber.Create(phoneNumber),
            _passwordHasher.Hash("password123"));

        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Rider));

        var profile = new RiderProfile(user, name);

        _db.Users.Add(user);
        _db.RiderProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user, App.RiderApp, riderProfile: profile);
        return new TestRider(user, profile, accessToken);
    }

    public async Task<TestDriver> CreateApprovedDriverAsync(
        string name = "Test Driver",
        string email = "driver@test.local",
        string phoneNumber = "+201000000002",
        string nationalId = "12345678901234",
        string licensePlate = "ABC1234",
        TestCoordinate? location = null,
        CancellationToken cancellationToken = default)
    {
        var user = new User(
            name,
            Email.Create(email),
            PhoneNumber.Create(phoneNumber),
            _passwordHasher.Hash("password123"));

        user.VerifyPhoneNumber();
        user.AddRole(new UserRole(RoleName.Driver));

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var profile = new DriverProfile(user.Id);
        profile.UpdatePersonalInfo(new DriverPersonalInfo(name, NationalId.Create(nationalId)));
        profile.UpdateVehicle(new DriverVehicleInfo(
            VehicleType.Scooter,
            "Test Brand",
            "Black",
            LicensePlate.Create(licensePlate),
            2024));

        profile.AddDocument(DocumentType.DrivingLicense, "test://driving-license", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)));
        profile.AddDocument(DocumentType.VehicleRegistration, "test://vehicle-registration");
        profile.AddDocument(DocumentType.DriverPhoto, "test://driver-photo");
        profile.SubmitForReview();

        _db.DriverProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        var approveResult = await _sender.Send(new ApproveDriverCommand(user.Id), cancellationToken);
        if (approveResult.IsError)
        {
            throw new InvalidOperationException(
                $"Failed to approve test driver: {string.Join(", ", approveResult.Errors.Select(error => error.Description))}");
        }

        await _db.Entry(profile).ReloadAsync(cancellationToken);

        var status = await _db.DriverStatuses.FirstAsync(ds => ds.UserId == user.Id, cancellationToken);
        var goOnlineResult = status.GoOnline();
        if (goOnlineResult.IsError)
        {
            throw new InvalidOperationException(
                $"Failed to put test driver online: {string.Join(", ", goOnlineResult.Errors.Select(error => error.Description))}");
        }

        var driverLocation = await _db.DriverLocations.FirstAsync(dl => dl.UserId == user.Id, cancellationToken);
        driverLocation.UpdateLocation(TestGeoFixtures.ToPoint(location ?? TestGeoFixtures.BenhaStation));

        var wallet = await _db.DriverWallets.FirstAsync(dw => dw.DriverUserId == user.Id, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user, App.DriverApp, driverProfile: profile);

        return new TestDriver(user, profile, status, driverLocation, wallet, accessToken);
    }
}
