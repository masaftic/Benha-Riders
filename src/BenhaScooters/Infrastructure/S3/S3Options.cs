using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.S3;

public class S3Options
{
    public const string SectionName = "S3";

    [Required]
    public string AccessKey { get; set; } = null!;

    [Required]
    public string SecretKey { get; set; } = null!;

    [Required]
    public string Region { get; set; } = null!;

    [Required]
    public string BucketName { get; set; } = null!;
    
    public string ServiceUrl { get; set; } = string.Empty; // Minio, or s3 compatible storage
    
    [Required]
    public string BaseUrl { get; set; } = null!; // Public base URL for direct file access
}
