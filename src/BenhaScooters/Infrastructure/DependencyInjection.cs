using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Infrastructure.Trips.Services;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Infrastructure.Matching.Services;
// using BenhaScooters.Infrastructure.Matching.BackgroundServices;
using BenhaScooters.Application.Services;
using BenhaScooters.Infrastructure.Interceptors;
using Hangfire;
using Hangfire.PostgreSql;
using BenhaScooters.Data;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Services;
using BenhaScooters.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BenhaScooters.Shared.Security;
using BenhaScooters.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using BenhaScooters.Presentation.Security;

namespace BenhaScooters.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure S3 options
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure FareEstimation options
        services.AddOptions<FareEstimationOptions>()
            .Bind(configuration.GetSection(FareEstimationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IFareEstimator, FareEstimator>();
        services.AddScoped<ITripFareService, TripFareService>();

        services.AddOptions<TripFareConfiguration>()
            .Bind(configuration.GetSection(TripFareConfiguration.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), o => o.UseNetTopologySuite()));

        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(o =>
            {
                o.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
            });
        });

        services.AddHangfireServer();

        // Register S3 client
        services.AddScoped<IAmazonS3>(sp =>
        {
            var s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.ServiceUrl,
                ForcePathStyle = s3Options.ForcePathStyle,
                AuthenticationRegion = s3Options.Region,
            };

            var credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);

            return new AmazonS3Client(credentials, config);
        });

        // Register S3 service
        services.AddScoped<IS3Service, S3Service>();

        // Register MinIO initialization service
        services.AddScoped<IMinioInitializationService, MinioInitializationService>();

        // Register Google authentication service
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // Register matching services
        services.AddScoped<IDriverRankingService, DriverRankingService>();

        // Register background services
        // services.AddHostedService<MatchTimeoutBackgroundService>();

        services.AddScoped<DataSeeder>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ISmsService, DevSmsService>();
        services.AddScoped<IFareEstimator, FareEstimator>();

        // Add the token cleanup background service
        services.AddHostedService<TokenCleanupService>();

        services.AddScoped<PublishDomainEventsInterceptor>();



        var jwtOptions = new JwtOptions();
        configuration.Bind(JwtOptions.SectionName, jwtOptions);

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
                };
            });

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("OnboardedDriver", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Driver");
                policy.RequireClaim(JwtClaims.DriverOnboardingStatus, "Completed");
            });

            opt.AddPolicy("RiderPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Rider");
                policy.RequireClaim(JwtClaims.RiderId);
                policy.RequireClaim(JwtClaims.Status, UserStatus.Active.ToString());
            });

            opt.AddPolicy("DriverPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Driver");
                policy.RequireClaim(JwtClaims.DriverId);
                policy.RequireClaim(JwtClaims.Status, UserStatus.Active.ToString());
            });
        });

        services.AddSingleton<IAuthorizationHandler, UserOnboardingRequirementHandler>();

        return services;
    }
}
