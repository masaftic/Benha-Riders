using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SmsVerificationCode> SmsVerificationCodes => Set<SmsVerificationCode>();

    public DbSet<Rider> Riders => Set<Rider>();

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<DriverAvailability> DriverAvailabilities => Set<DriverAvailability>();

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripGpsPoint> TripGpsPoints => Set<TripGpsPoint>();
    public DbSet<TripRoute> TripRoutes => Set<TripRoute>();
    public DbSet<TripFare> TripFares => Set<TripFare>();
    public DbSet<TripRequest> TripRequests => Set<TripRequest>();

    public DbSet<DriverMatchAttempt> DriverMatchAttempts => Set<DriverMatchAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.RegisterAllInVogenEfCoreConverters();
    }
}
