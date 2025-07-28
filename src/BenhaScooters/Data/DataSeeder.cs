using System.Threading.Tasks;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;

namespace BenhaScooters.Data;

public class DataSeeder(AppDbContext db, IPasswordHasher passwordHasher)
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

                user.AddRole(new UserRole(RoleName.Admin));

                user.VerifyPhoneNumber();
                user.UpdateStatus(UserStatus.Active).Else(e =>
                {
                    throw new Exception("Failed to update user status: " + e.First().Description);
                    return e.First();
                });

                db.Users.Add(user);

                await db.SaveChangesAsync();
            }

            // create a verified driver
            if (!db.Drivers.Any(dp => dp.Info!.FullName == "Sample Driver"))
            {
                var user = new User(
                    "Driver",
                    Email.From("driver@gmail.com"),
                    PhoneNumber.From("01234567891"),
                    passwordHasher.Hash("password"));

                user.AddRole(new UserRole(RoleName.Driver));
                user.VerifyPhoneNumber();
                user.UpdateStatus(UserStatus.Active).Else(e =>
                {
                    throw new Exception("Failed to update user status: " + e.First().Description);
                    return e.First();
                });

                db.Users.Add(user);

                await db.SaveChangesAsync();

                var driver = new Driver(user.Id);

                var result = driver
                    .UpdatePersonalInfo("Sample Driver", NationalId.From("12345678901234"), new DateOnly(1990, 1, 1), "123 Street", "City", "Emergency Contact", PhoneNumber.From("09876543213"))
                    .Then(res => driver.AddVehicle(VehicleType.Scooter, "Brand", "Model", "Color", LicensePlate.From("ABC1234"), 2020, VIN.From("12345678901234567")))
                    .Then(res => driver.AddDocument(DocumentType.DrivingLicense, "url://image1", DateTime.UtcNow.AddYears(5))
                    .Then(res => driver.AddDocument(DocumentType.VehicleRegistration, "url://image2", DateTime.UtcNow.AddYears(1)))
                    .Then(res => driver.AddDocument(DocumentType.DriverPhoto, "url://image3", DateTime.UtcNow.AddYears(10)))
                    .Then(res => driver.CompleteOnboarding()));

                if (result.IsError)
                {
                    throw new Exception("Failed to create driver: " + result.Errors.First().Description);
                }

                db.Drivers.Add(driver);

                await db.SaveChangesAsync();
            }

            if (!db.Riders.Any(r => r.PreferredName == "Sample Rider"))
            {
                var user = new User(
                    "Rider",
                    Email.From("rider@gmail.com"),
                    PhoneNumber.From("01234567893"),
                    passwordHasher.Hash("password"));

                user.AddRole(new UserRole(RoleName.Rider));
                user.VerifyPhoneNumber();
                user.UpdateStatus(UserStatus.Active).Else(e =>
                {
                    throw new Exception("Failed to update user status: " + e.First().Description);
                    return e.First();
                });

                db.Users.Add(user);

                await db.SaveChangesAsync();
                var rider = new Rider(user.Id, "Sample Rider");

                db.Riders.Add(rider);

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