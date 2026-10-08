namespace BenhaScooters.IntegrationTests.Infrastructure;

// Infrastructure/RespawnResetter.cs
using Respawn;
using Npgsql;

public sealed class RespawnResetter
{
    private readonly Respawner _respawner;
    private readonly NpgsqlConnection _connection;

    public RespawnResetter(NpgsqlConnection connection)
    {
        _connection = connection;

        _respawner = Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore =
            [
                new Respawn.Graph.Table("__EFMigrationsHistory"),
                new Respawn.Graph.Table("spatial_ref_sys")
            ]
        }).GetAwaiter().GetResult();
    }

    public Task ResetAsync()
        => _respawner.ResetAsync(_connection);
}
