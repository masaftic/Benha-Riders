using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BenhaScooters.Data;
using System.Net.Http.Json;
using System.Text.Json;
using BenhaScooters.Features.Authentication;
using Testcontainers.PostgreSql;

namespace BenhaScooters.IntegrationTests;

public class IntegrationTestBase : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    protected readonly DatabaseFixture DatabaseFixture;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected IServiceScope? Scope;
    protected AppDbContext? DbContext;

    public IntegrationTestBase(DatabaseFixture databaseFixture)
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
            });
        });

        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure database is created and migrated
        await DbContext.Database.MigrateAsync();
        
        // Clean up any existing data
        await CleanupDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await CleanupDatabaseAsync();
        }
        
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
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
    /// Registers a user and returns the registration response
    /// </summary>
    protected async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<RegisterResponse>(response) ?? throw new InvalidOperationException("Failed to deserialize register response");
    }

    /// <summary>
    /// Registers a user and verifies their phone number, returning the user
    /// </summary>
    protected async Task<RegisterResponse> RegisterAndVerifyUserAsync(RegisterRequest request)
    {
        var registerResponse = await RegisterUserAsync(request);
        
        // Skip SMS verification by directly updating the database
        var user = await DbContext!.Users.FindAsync(registerResponse.UserId);
        if (user != null)
        {
            user.VerifyPhoneNumber();
            await DbContext.SaveChangesAsync();
        }

        return registerResponse;
    }

    /// <summary>
    /// Logs in a user and returns the login response
    /// </summary>
    protected async Task<LoginResponse> LoginUserAsync(LoginRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await DeserializeResponse<LoginResponse>(response) ?? throw new InvalidOperationException("Failed to deserialize login response");
    }

    /// <summary>
    /// Registers, verifies, and logs in a user, then sets the authorization header
    /// </summary>
    protected async Task<(RegisterResponse registerResponse, LoginResponse loginResponse)> RegisterVerifyAndLoginUserAsync(RegisterRequest registerRequest)
    {
        var registerResponse = await RegisterAndVerifyUserAsync(registerRequest);
        
        var loginRequest = TestDataFactory.CreateLoginRequest(registerRequest.Email, registerRequest.Password);
        var loginResponse = await LoginUserAsync(loginRequest);
        
        SetAuthorizationHeader(loginResponse.AccessToken);
        
        return (registerResponse, loginResponse);
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
