using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, PublishDomainEventsInterceptor publishDomainEventsInterceptor) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalAuth> ExternalAuths => Set<ExternalAuth>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SmsVerificationCode> SmsVerificationCodes => Set<SmsVerificationCode>();

    public DbSet<Rider> Riders => Set<Rider>();

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverVehicle> Vehicles => Set<DriverVehicle>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<DriverAvailability> DriverAvailabilities => Set<DriverAvailability>();
    public DbSet<DriverRating> DriverRatings => Set<DriverRating>();

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripGpsPoint> TripGpsPoints => Set<TripGpsPoint>();
    public DbSet<TripRoute> TripRoutes => Set<TripRoute>();

    public DbSet<TripRequest> TripRequests => Set<TripRequest>();

    public DbSet<DriverMatchAttempt> DriverMatchAttempts => Set<DriverMatchAttempt>();
    public DbSet<MatchingSession> MatchingSessions => Set<MatchingSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.Ignore<List<IDomainEvent>>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.RegisterAllInVogenEfCoreConverters();

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(publishDomainEventsInterceptor);

        base.OnConfiguring(optionsBuilder);
    }
}
