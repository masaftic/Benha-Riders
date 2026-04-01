using BenhaScooters.Application;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Infrastructure;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Presentation;
using BenhaScooters.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// Configure Serilog for file logging with 14-day retention
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/benha-scooters-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Benha Scooters application");

    // Use Serilog for logging
    builder.Host.UseSerilog();

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

    // Serve uploaded files (driver photos/documents) from the uploads folder at '/files' path
    // This must come BEFORE the Angular app's UseStaticFiles to avoid conflicts
    app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/files"
    });

    // Serve static files from wwwroot/browser (Angular build output)
    var browserPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "browser");
    if (Directory.Exists(browserPath))
    {
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(browserPath)
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(browserPath)
        });
    }

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

    RecurringJob.AddOrUpdate<TokenCleanupService>(
        "token-cleanup",
        job => job.CleanupExpiredTokensAsync(),
        Cron.Hourly(1));

    RecurringJob.AddOrUpdate<HungTripCleanupService>(
        "hung-trip-cleanup",
        job => job.CleanupHungTripsAsync(),
        Cron.Hourly(2));

    RecurringJob.AddOrUpdate<InactiveDriverCleanupService>(
        "inactive-driver-cleanup",
        job => job.SetInactiveDriversOfflineAsync(),
        Cron.Hourly(3));

    RecurringJob.AddOrUpdate<MatchingCleanupService>(
        "matching-cleanup",
        job => job.CleanupOldOffersAsync(),
        Cron.Hourly(4));

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapHub<DriverHub>("/hubs/driver");
    app.MapHub<RiderHub>("/hubs/rider");

    // SPA fallback - serve index.html from browser subdirectory for client-side routing
    app.MapFallbackToFile("browser/index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public for integration tests
namespace BenhaScooters
{
    public partial class Program { }
}
