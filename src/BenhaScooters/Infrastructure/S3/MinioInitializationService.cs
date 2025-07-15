using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.S3;

public interface IMinioInitializationService
{
    Task InitializeAsync();
}

public class MinioInitializationService : IMinioInitializationService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;
    private readonly ILogger<MinioInitializationService> _logger;

    public MinioInitializationService(
        IAmazonS3 s3Client, 
        IOptions<S3Options> s3Options,
        ILogger<MinioInitializationService> logger)
    {
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Check if bucket exists
            await _s3Client.EnsureBucketExistsAsync(_s3Options.BucketName);
            _logger.LogInformation("S3 bucket initialized: {BucketName}", _s3Options.BucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize S3 bucket: {BucketName}", _s3Options.BucketName);
            throw;
        }
    }
}
