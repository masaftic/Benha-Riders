using BenhaScooters.Data;
using BenhaScooters.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace BenhaScooters.IntegrationTests.Fixtures;

// Fixtures/TestFixture.cs
public sealed class TestFixture : IAsyncLifetime
{
    private readonly DatabaseContainer _db = new();
    private RespawnResetter _respawner = null!;
    private NpgsqlConnection _connection = null!;
    private TestApiFactory _factory = null!;

    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        // 1. Build the database
        await _db.StartAsync();

        // 2. Build app factory once
        _factory = new TestApiFactory(_db.ConnectionString);
        Client = _factory.CreateClient();

        // 3. Run migrations once
        using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();

        // 4. Setup respawn once
        _connection = new NpgsqlConnection(_db.ConnectionString);
        await _connection.OpenAsync();

        _respawner = new RespawnResetter(_connection);
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync();
    }

    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_db.ConnectionString, o => o.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();

        if (_connection is not null)
            await _connection.DisposeAsync();

        await _db.DisposeAsync();
    }
}
