using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using BenhaScooters.Infrastructure.S3;

namespace BenhaScooters.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure S3 options
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
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

        return services;
    }
}
