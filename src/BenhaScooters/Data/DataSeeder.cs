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

                user.VerifyPhoneNumber();

                user.AddRole(new UserRole(RoleName.Admin));

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

                user.VerifyPhoneNumber();

                user.AddRole(new UserRole(RoleName.Driver));

                db.Users.Add(user);
                await db.SaveChangesAsync();

                var driver = new Driver(user.Id);

                db.Drivers.Add(driver);
                await db.SaveChangesAsync();

                var result = driver
                    .UpdatePersonalInfo("Sample Driver", NationalId.From("12345678901234"), new DateOnly(1990, 1, 1), "123 Street", "City", "Emergency Contact", PhoneNumber.From("09876543213"))
                    .Then(res => driver.EnrollVehicle(VehicleType.Scooter, "Brand", "Model", "Color", LicensePlate.From("ABC1234"), 2020, VIN.From("12345678901234567")))
                    .Then(res => driver.AddDocument(DocumentType.DrivingLicense, "url://image1", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5)))
                    .Then(res => driver.AddDocument(DocumentType.VehicleRegistration, "url://image2"))
                    .Then(res => driver.AddDocument(DocumentType.DriverPhoto, "url://image3"))
                    .Then(res => driver.CompleteOnboarding()));

                if (result.IsError)
                {
                    throw new Exception("Failed to create driver: " + result.Errors.First().Description);
                }

                await db.SaveChangesAsync();
            }

            if (!db.Riders.Any(r => r.PreferredName == "Sample Rider"))
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