using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Ratings;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.ServiceAreas;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Thinktecture;

namespace BenhaScooters.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, PublishDomainEventsInterceptor publishDomainEventsInterceptor) : DbContext(options)
{
    // User & Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalAuth> ExternalAuths => Set<ExternalAuth>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SmsVerificationCode> SmsVerificationCodes => Set<SmsVerificationCode>();
    public DbSet<OtpSecurityEvent> OtpSecurityEvents => Set<OtpSecurityEvent>();
    public DbSet<SmsLog> SmsLogs => Set<SmsLog>();

    // Rider (new structure)
    public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();

    // Driver (new structure - tiered by update frequency)
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();  // Static/rare updates
    public DbSet<DriverStatus> DriverStatuses => Set<DriverStatus>();    // Medium frequency
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>(); // High frequency
    public DbSet<DriverStats> DriverStats => Set<DriverStats>();         // Medium frequency
    public DbSet<DriverWallet> DriverWallets => Set<DriverWallet>();     // Wallet/transactions
    public DbSet<WalletTopUpRequest> WalletTopUpRequests => Set<WalletTopUpRequest>(); // Top-up requests

    // Trips
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripGpsPoint> TripGpsPoints => Set<TripGpsPoint>();
    public DbSet<TripRoute> TripRoutes => Set<TripRoute>();
    public DbSet<TripRequest> TripRequests => Set<TripRequest>();

    // Matching
    public DbSet<DriverMatchAttempt> DriverMatchAttempts => Set<DriverMatchAttempt>();
    public DbSet<MatchingSession> MatchingSessions => Set<MatchingSession>();

    // Push Notifications
    public DbSet<UserDeviceToken> UserDeviceTokens => Set<UserDeviceToken>();

    // Ratings
    public DbSet<DriverRating> DriverRatings => Set<DriverRating>();


    // Service Area
    public DbSet<ServiceArea> ServiceAreas => Set<ServiceArea>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddThinktectureValueConverters();
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.Ignore<List<IDomainEvent>>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(publishDomainEventsInterceptor);

        base.OnConfiguring(optionsBuilder);
    }
}
