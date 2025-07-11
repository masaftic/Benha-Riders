using System.Threading.Tasks;
using BenhaScooters.Domain;
using BenhaScooters.Features.Authentication.Services;

namespace BenhaScooters.Data;

public class DataSeeder(AppDbContext db, IPasswordHasher passwordHasher)
{
    public async Task SeedAsync()
    {
        if (!db.Users.Any(u => u.Name == "John Doe"))
        {
            var user = new User(
                "John Doe",
                Email.From("John@gmail.com"),
                PhoneNumber.From("01234567890"),
                passwordHasher.Hash("password"));

            user.AddRole(new UserRole(RoleName.Admin));

            user.VerifyPhoneNumber();

            db.Users.Add(user);

            await db.SaveChangesAsync();
        }
    }
}
