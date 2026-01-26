using System.Threading.Tasks;
using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;

namespace BenhaScooters.Data;

public class DataSeeder(AppDbContext db, IPasswordHasher passwordHasher, ISender sender)
{
    public async Task SeedAsync()
    {
        var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            if (!db.Users.Any(u => u.Name == "Admin"))
            {
                var user = new User(
                    "Admin",
                    Email.From("admin@gmail.com"),
                    PhoneNumber.From("01234567890"),
                    passwordHasher.Hash("password"));

                user.VerifyPhoneNumber();

                user.AddRole(new UserRole(RoleName.Admin));

                db.Users.Add(user);

                await db.SaveChangesAsync();
            }

            // create a verified driver
            if (!db.DriverProfiles.Any(dp => dp.PersonalInfo!.FullName == "Sample Driver"))
            {
                var user = new User(
                    "Driver",
                    Email.From("driver@gmail.com"),
                    PhoneNumber.From("01234567891"),
                    passwordHasher.Hash("password"));

                user.VerifyPhoneNumber();

                user.AddRole(new UserRole(RoleName.Driver));

                db.Users.Add(user);
                await db.SaveChangesAsync();

                var driverProfile = new DriverProfile(user.Id);
                
                var personalInfo = new DriverPersonalInfo(
                    "Sample Driver", 
                    NationalId.From("12345678901234"), 
                    new DateOnly(1990, 1, 1), 
                    "123 Street", 
                    "City", 
                    "Emergency Contact", 
                    PhoneNumber.From("09876543213"));
                
                var vehicleInfo = new DriverVehicleInfo(
                    VehicleType.Scooter, 
                    "Brand", 
                    "Model", 
                    "Color", 
                    LicensePlate.From("ABC1234"), 
                    2020, 
                    VIN.From("12345678901234567"));
                
                driverProfile.UpdatePersonalInfo(personalInfo);
                driverProfile.UpdateVehicle(vehicleInfo);
                driverProfile.AddDocument(DocumentType.DrivingLicense, "url://image1", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5)));
                driverProfile.AddDocument(DocumentType.VehicleRegistration, "url://image2");
                driverProfile.AddDocument(DocumentType.DriverPhoto, "url://image3");

                driverProfile.SubmitForReview();
                db.DriverProfiles.Add(driverProfile);

                await db.SaveChangesAsync();
                
                await sender.Send(new ApproveDriverCommand(user.Id)); // automatically approve the driver
                
                await db.SaveChangesAsync();
            }

            if (!db.RiderProfiles.Any(r => r.PreferredName == "Sample Rider"))
            {
                var user = new User(
                    "Rider",
                    Email.From("rider@gmail.com"),
                    PhoneNumber.From("01234567893"),
                    passwordHasher.Hash("password"));

                user.VerifyPhoneNumber();

                user.AddRole(new UserRole(RoleName.Rider));

                db.Users.Add(user);

                await db.SaveChangesAsync();
                var riderProfile = new RiderProfile(user.Id, "Sample Rider");

                db.RiderProfiles.Add(riderProfile);

                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}