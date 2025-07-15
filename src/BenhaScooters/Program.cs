using System.Text.Json.Serialization;
using BenhaScooters.Data;
using BenhaScooters.Infrastructure;
using BenhaScooters.Infrastructure.Authentication;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Infrastructure.Trips.Services;
using BenhaScooters.Services;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseNetTopologySuite()));

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ISmsService, DevSmsService>();
builder.Services.AddScoped<IFareEstimator, FareEstimator>();

// Add the token cleanup background service
builder.Services.AddHostedService<TokenCleanupService>();

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthenticationJwtBearer(s =>
        s.SigningKey = builder.Configuration[JwtOptions.SectionName + ":SigningKey"]
        ?? throw new InvalidOperationException("JWT Signing Key is not configured."))
    .AddAuthentication();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.EnvironmentName != "Testing")
    {
        // Apply migrations and seed data only in non-testing environments
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await dataSeeder.SeedAsync();
        
        // Initialize MinIO bucket
        var minioInitService = scope.ServiceProvider.GetRequiredService<IMinioInitializationService>();
        await minioInitService.InitializeAsync();
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultExceptionHandler().UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Errors.UseProblemDetails(x =>
    {
        x.IndicateErrorCode = true; 
    });
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
}).UseSwaggerGen();


app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
