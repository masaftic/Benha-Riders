using System.Threading.Tasks;
using BenhaScooters.Domain;

namespace BenhaScooters.Data;

public class DataSeeder(AppDbContext db)
{
    public async Task SeedAsync()
    {
        if (!db.Users.Any())
        {
            var user = new User(
                UserId.From(Guid.NewGuid()),
                "John Doe",
                Email.From("John@gmail.com"),
                PhoneNumber.From("01234567890"),
                "hashed_password");

            user.AddRole(new UserRole(user.Id, RoleName.Admin));

            db.Users.Add(user);

            await db.SaveChangesAsync();
        }
    }
}
