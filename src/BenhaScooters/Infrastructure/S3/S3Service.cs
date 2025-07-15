using Amazon.S3;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Infrastructure.S3;

public interface IS3Service
{
    Task<string> UploadFileAsync(IFormFile file, string keyPrefix, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetPreSignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;

    public S3Service(IAmazonS3 s3Client, IOptions<S3Options> s3Options)
    {
        _s3Client = s3Client;
        _s3Options = s3Options.Value;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string keyPrefix, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required and cannot be empty.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException($"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

        var maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
            throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB.");

        // Generate unique file key
        var fileName = $"{Guid.NewGuid()}{extension}";
        var key = string.IsNullOrEmpty(keyPrefix) ? fileName : $"{keyPrefix}/{fileName}";

        using var stream = file.OpenReadStream();
        
        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = _s3Options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            // ServerSideEncryptionMethod = Amazon.S3.ServerSideEncryptionMethod.AES256
        };

        var response = await _s3Client.PutObjectAsync(request, cancellationToken);
        
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            throw new InvalidOperationException($"Failed to upload file to S3. Status: {response.HttpStatusCode}");

        return key;
    }

    public async Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new Amazon.S3.Model.DeleteObjectRequest
            {
                BucketName = _s3Options.BucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request, cancellationToken);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetPreSignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var request = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = _s3Options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = Amazon.S3.HttpVerb.GET,
            // use http
            Protocol = Amazon.S3.Protocol.HTTP
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }
}
