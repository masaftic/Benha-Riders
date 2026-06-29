using Testcontainers.PostgreSql;

namespace BenhaScooters.IntegrationTests.Infrastructure;

public class DatabaseContainer
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:16-3.5")
        .WithDatabase("benha_scooters_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task StartAsync() => _container.StartAsync();
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
