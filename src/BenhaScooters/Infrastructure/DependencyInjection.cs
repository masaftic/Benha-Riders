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
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.GoogleMaps;
using BenhaScooters.Infrastructure.Security;
using System.Net.Http.Headers;
using BenhaScooters.Infrastructure.Notifications;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using BenhaScooters.Data.MapData;
using System.Security.Claims;

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

        services.AddMemoryCache();

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

        // Register SignalR connection tracker (singleton to persist across requests)
        services.AddSingleton<ISignalRConnectionTracker, SignalRConnectionTracker>();

        // Register background services
        // services.AddHostedService<MatchTimeoutBackgroundService>();

        services.AddScoped<DataSeeder>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();

        // SMS Service Configuration
        services.AddOptions<SmsOptions>()
            .Bind(configuration.GetSection(SmsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register SMS service conditionally based on configuration
        var smsOptions = configuration.GetSection(SmsOptions.SectionName).Get<SmsOptions>();
        if (smsOptions?.EnableRealSmsSending == true)
        {
            services.AddHttpClient<ISmsService, WhySmsSenderService>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<SmsOptions>>().Value;
                    client.BaseAddress = new Uri(options.ApiUrl);
                    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
                });
        }
        else
        {
            services.AddScoped<ISmsService, DummySmsService>();
        }

        // OTP Security
        services.AddOptions<OtpSecurityOptions>()
            .Bind(configuration.GetSection(OtpSecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<OtpRateLimiter>();
        services.AddScoped<IOtpRateLimiter>(sp => sp.GetRequiredService<OtpRateLimiter>());
        services.AddScoped<IOtpFraudDetector, OtpFraudDetector>();
        services.AddScoped<IOtpSecurityService, OtpSecurityService>();
        services.AddScoped<IFareEstimator, FareEstimator>();
        services.AddScoped<IGeoService, GeoService>();
        services.AddScoped<ServiceAreasPolygonSeeder>();
        services.AddScoped<IServiceAreaValidator, ServiceAreaValidator>();

        services.AddScoped<TokenCleanupService>();
        services.AddScoped<MatchingCleanupService>();
        services.AddScoped<HungTripCleanupService>();
        services.AddScoped<InactiveDriverCleanupService>();

        services.AddScoped<PublishDomainEventsInterceptor>();

        // Firebase Cloud Messaging (FCM)
        var firebaseCredentialPath = configuration["Firebase:CredentialPath"];
        if (!string.IsNullOrEmpty(firebaseCredentialPath) && File.Exists(firebaseCredentialPath))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebaseCredentialPath)
            });
            services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
        }
        else
        {
            // No-op implementation when Firebase is not configured
            services.AddScoped<IPushNotificationService, NoOpPushNotificationService>();
        }

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
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.Zero
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
                    },
                    OnTokenValidated = async context =>
                    {
                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier);
                        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userIdValue))
                        {
                            context.Fail("Invalid user identifier.");
                            return;
                        }

                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var isActive = await dbContext.Users
                            .Where(u => u.Id == UserId.Create(userIdValue))
                            .Select(u => u.IsActive)
                            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                        if (!isActive)
                        {
                            context.Fail("User account is deactivated.");
                        }
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyConstants.PhoneVerifiedPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtClaims.PhoneVerified, "true");
            })
            .AddPolicy(PolicyConstants.ApprovedDriverPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtClaims.App, AppExtensions.DriverAppValue);
                policy.RequireClaim(JwtClaims.PhoneVerified, "true");
                policy.RequireClaim(JwtClaims.DriverOnboardingStatus, DriverOnboardingStatus.Approved.ToString());
            })
            .AddPolicy(PolicyConstants.RiderPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtClaims.App, AppExtensions.RiderAppValue);
                policy.RequireClaim(JwtClaims.PhoneVerified, "true");
            })
            .AddPolicy(PolicyConstants.DriverPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtClaims.App, AppExtensions.DriverAppValue);
                policy.RequireClaim(JwtClaims.PhoneVerified, "true");
            });

        return services;
    }
}
