namespace BenhaScooters.Infrastructure.S3;

public class S3Options
{
    public const string SectionName = "S3";
    
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}
