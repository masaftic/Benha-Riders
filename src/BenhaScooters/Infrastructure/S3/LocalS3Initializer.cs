using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.S3;

public class LocalS3Initializer : IS3InitializationService
{
    private readonly S3Options _options;
    private readonly ILogger<LocalS3Initializer> _logger;

    public LocalS3Initializer(IOptions<S3Options> options, ILogger<LocalS3Initializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        try
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", _options.BucketName ?? "default");
            Directory.CreateDirectory(basePath);
            _logger.LogInformation("Local uploads directory ready: {Path}", basePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize local uploads directory");
        }

        return Task.CompletedTask;
    }
}
