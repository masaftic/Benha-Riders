using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Infrastructure.Trips.Services;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Infrastructure.Matching.Services;
using BenhaScooters.Infrastructure.Matching.BackgroundServices;
using BenhaScooters.Application.Services;
using BenhaScooters.Infrastructure.Interceptors;

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
        services.AddHostedService<MatchTimeoutBackgroundService>();


        services.AddScoped<PublishDomainEventsInterceptor>();

        return services;
    }
}
