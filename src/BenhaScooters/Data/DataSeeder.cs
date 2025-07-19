using System.Threading.Tasks;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
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
        if (!db.Users.Any(u => u.Name == "Admin"))
        {
            var user = new User(
                "Admin",
                Email.From("admin@gmail.com"),
                PhoneNumber.From("01234567890"),
                passwordHasher.Hash("password"));

            user.AddRole(new UserRole(RoleName.Admin));

            user.VerifyPhoneNumber();

            db.Users.Add(user);

            await db.SaveChangesAsync();
        }

        // create a verified driver
        if (!db.Drivers.Any(dp => dp.PersonalInfo!.FullName == "Sample Driver"))
        {
            var user = new User(
                "Driver",
                Email.From("driver@gmail.com"),
                PhoneNumber.From("01234567890"),
                passwordHasher.Hash("password"));

            user.AddRole(new UserRole(RoleName.Driver));
            user.VerifyPhoneNumber();

            db.Users.Add(user);

            await db.SaveChangesAsync();

            var driver = new Driver(user.Id);

            driver.UpdatePersonalInfo("Sample Driver", NationalId.From("12345678901234"),
                new DateOnly(1990, 1, 1), "123 Street", "City", "Emergency Contact",
                PhoneNumber.From("09876543210"));

            driver.UpdateVehicleInfo(
                VehicleType.Scooter, "Brand", "Model", "Color",
                LicensePlate.From("ABC1234"), 2020);

            driver.UpdateDocuments("http://example", "http://example", "http://example");

            driver.CompleteOnboarding();

            db.Drivers.Add(driver);

            await db.SaveChangesAsync();
        }

        if (!db.Riders.Any(r => r.PreferredName == "Sample Rider"))
        {
            var user = new User(
                "Rider",
                Email.From("rider@gmail.com"),
                PhoneNumber.From("01234567890"),
                passwordHasher.Hash("password"));

            user.AddRole(new UserRole(RoleName.Rider));
            user.VerifyPhoneNumber();

            db.Users.Add(user);

            await db.SaveChangesAsync();
            var rider = new Rider(user.Id, "Sample Rider");

            db.Riders.Add(rider);

            await db.SaveChangesAsync();
        }
    }
}