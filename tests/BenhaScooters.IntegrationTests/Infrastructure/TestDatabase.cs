using BenhaScooters.Data;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.IntegrationTests.Infrastructure;

public sealed class TestDatabase
{
    public AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, o => o.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }

    public void Migrate(string connectionString)
    {
        using var ctx = CreateContext(connectionString);
        ctx.Database.Migrate();
    }
}
