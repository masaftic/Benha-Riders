using BenhaScooters.Application;
using BenhaScooters.Data;
using BenhaScooters.Infrastructure;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Presentation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Vogen;

[assembly: VogenDefaults(
 openApiSchemaCustomizations: OpenApiSchemaCustomizations.GenerateSwashbuckleMappingExtensionMethod)]


var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5000);
});

// Add infrastructure services
builder.Services.AddPresentation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();



var app = builder.Build();

app.UseCors();

app.UseRateLimiter();

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

app.UseHangfireDashboard();
app.MapHangfireDashboard();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();


app.Run();

// Make the implicit Program class public for integration tests
namespace BenhaScooters
{
    public partial class Program { }
}
