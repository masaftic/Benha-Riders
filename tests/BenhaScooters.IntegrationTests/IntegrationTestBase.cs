using BenhaScooters.IntegrationTests.Fixtures;
using BenhaScooters.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Thinktecture.Text.Json.Serialization;

namespace BenhaScooters.IntegrationTests;

public class IntegrationTestBase : IAsyncLifetime
{
    protected readonly TestFixture Fixture;
    private IServiceScope? _scope;

    protected IntegrationTestBase(TestFixture fixture)
    {
        Fixture = fixture;
    }

    protected HttpClient Client => Fixture.Client;
    protected IServiceProvider Services => _scope?.ServiceProvider
        ?? throw new InvalidOperationException("The test service scope has not been initialized yet.");

    protected AppDbContext DbContext => Services.GetRequiredService<AppDbContext>();

    public async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
        _scope = Fixture.CreateScope();
        ClearAuthorizationHeader();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _scope = null;
        ClearAuthorizationHeader();

        return Task.CompletedTask;
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    protected static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    protected static async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected void SetAuthorizationHeader(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    protected void ClearAuthorizationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ThinktectureJsonConverterFactory());

        return options;
    }
}
