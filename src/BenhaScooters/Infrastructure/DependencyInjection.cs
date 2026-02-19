// Local filesystem used instead of S3
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Infrastructure.Trips.Services;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Infrastructure.Matching.Services;
using BenhaScooters.Infrastructure.Matching.Settings;
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
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.GoogleMaps;

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


        services.AddMemoryCache();

        services.AddOptions<TripFareConfiguration>()
            .Bind(configuration.GetSection(TripFareConfiguration.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure driver ranking options (search radius, etc.)
        services.AddOptions<DriverRankingOptions>()
            .Bind(configuration.GetSection(DriverRankingOptions.SectionName))
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

        // Register local filesystem-based S3 replacement
        services.AddScoped<IS3Service, LocalS3Service>();
        services.AddScoped<IS3InitializationService, LocalS3Initializer>();

        // Register Google authentication service
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // Configure Google Maps options and register service
        services.AddOptions<GoogleMapsOptions>()
            .Bind(configuration.GetSection(GoogleMapsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IGoogleMapsService, GoogleMapsService>();

        // Register matching services
        services.AddScoped<IDriverRankingService, DriverRankingService>();

        // Register background services
        // services.AddHostedService<MatchTimeoutBackgroundService>();

        services.AddScoped<DataSeeder>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ISmsService, DevSmsService>();
        services.AddScoped<IFareEstimator, FareEstimator>();
        services.AddScoped<IGeoService, GeoService>();

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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Allow JWT tokens to be received from query string for SignalR hubs
                        if (context.Request.Path.StartsWithSegments("/hubs") &&
                            context.Request.Query.ContainsKey("access_token"))
                        {
                            context.Token = context.Request.Query["access_token"];
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("OnboardedDriver", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Driver");
                policy.RequireClaim(JwtClaims.DriverOnboardingStatus, DriverOnboardingStatus.Approved.ToString());
            });

            opt.AddPolicy("RiderPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Rider");
                policy.RequireClaim(JwtClaims.Status, UserStatus.Active.ToString());
            });

            opt.AddPolicy("DriverPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Driver");
                policy.RequireClaim(JwtClaims.Status, UserStatus.Active.ToString());
            });


            opt.AddPolicy("PhoneVerified", policy =>
            {
                policy.AddRequirements(new UserOnboardingRequirement(OnboardingSteps.VerifyPhone));
            });

            opt.AddPolicy("RoleSelected", policy =>
            {
                policy.AddRequirements(new UserOnboardingRequirement(OnboardingSteps.SelectRole));
            });
        });

        services.AddSingleton<IAuthorizationHandler, UserOnboardingRequirementHandler>();

        return services;
    }
}
