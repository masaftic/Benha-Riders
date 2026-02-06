using BenhaScooters.Application;
using BenhaScooters.Data;
using BenhaScooters.Infrastructure;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Presentation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5000);
});


// Add infrastructure services
builder.Services.AddPresentation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);



var app = builder.Build();

app.UseCors();

app.UseRateLimiter();


// Ensure uploads directory exists for PhysicalFileProvider
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Serve uploaded files from the local uploads folder at the '/files' path
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/files"
});

using (var scope = app.Services.CreateScope())
{
    if (app.Environment.EnvironmentName != "Testing")
    {
        // Apply migrations and seed data only in non-testing environments
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await dataSeeder.SeedAsync();

        // Initialize S3 bucket
        var s3InitService = scope.ServiceProvider.GetRequiredService<IS3InitializationService>();
        await s3InitService.InitializeAsync();
    }
}

app.UseHangfireDashboard();
app.MapHangfireDashboard();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapEndpoints();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.MapHub<DriverHub>("/hubs/driver");
app.MapHub<RiderHub>("/hubs/rider");


app.Run();

// Make the implicit Program class public for integration tests
namespace BenhaScooters
{
    public partial class Program { }
}
