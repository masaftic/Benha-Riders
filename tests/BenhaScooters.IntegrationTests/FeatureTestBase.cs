using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BenhaScooters.Data;
using System.Net.Http.Json;
using System.Text.Json;

namespace BenhaScooters.IntegrationTests;

/// <summary>
/// Base class for all feature integration tests
/// </summary>
public abstract class FeatureTestBase : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DatabaseFixture;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected IServiceScope? Scope;
    protected AppDbContext? DbContext;

    protected FeatureTestBase(DatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
        
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registrations
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                if (contextDescriptor != null)
                    services.Remove(contextDescriptor);

                // Add PostgreSQL database for testing using Testcontainers connection string
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(databaseFixture.ConnectionString);
                });

                // Allow derived classes to configure additional services
                ConfigureTestServices(services);
            });
        });

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Override this method to configure additional services for specific feature tests
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Default implementation does nothing
    }

    public async Task InitializeAsync()
    {
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure database is created and migrated
        await DbContext.Database.MigrateAsync();
        
        // Clean up any existing data
        await CleanupDatabaseAsync();

        // Allow derived classes to perform additional initialization
        await OnInitializeAsync();
    }

    /// <summary>
    /// Override this method to perform additional initialization for specific feature tests
    /// </summary>
    protected virtual Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Allow derived classes to perform cleanup before disposal
        await OnDisposeAsync();

        if (DbContext != null)
        {
            await CleanupDatabaseAsync();
        }
        
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }

    /// <summary>
    /// Override this method to perform additional cleanup for specific feature tests
    /// </summary>
    protected virtual Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task CleanupDatabaseAsync()
    {
        if (DbContext == null) return;

        // Clean up test data in reverse order of dependencies
        DbContext.SmsVerificationCodes.RemoveRange(DbContext.SmsVerificationCodes);
        DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
        DbContext.UserRoles.RemoveRange(DbContext.UserRoles);
        DbContext.Users.RemoveRange(DbContext.Users);
        
        await DbContext.SaveChangesAsync();
    }

    protected static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    /// <summary>
    /// Sets the authorization header for subsequent requests
    /// </summary>
    protected void SetAuthorizationHeader(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    /// <summary>
    /// Clears the authorization header
    /// </summary>
    protected void ClearAuthorizationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    public void Dispose()
    {
        Scope?.Dispose();
        Client?.Dispose();
    }
}
