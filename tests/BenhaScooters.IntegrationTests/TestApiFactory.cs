using BenhaScooters.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BenhaScooters.IntegrationTests;

public class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            var config = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["GoogleMaps:ApiKey"] = "TestApiKey"
            };

            cfg.AddInMemoryCollection(config);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // replace with test DB
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString, o => o.UseNetTopologySuite());
            });
        });
    }
}
