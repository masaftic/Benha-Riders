using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.StaticFiles;

namespace BenhaScooters.Infrastructure.S3;

public class LocalS3Service : IS3Service
{
    private readonly S3Options _options;
    private readonly string _basePath;

    public LocalS3Service(IOptions<S3Options> options)
    {
        _options = options.Value;
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", _options.BucketName ?? "default");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<ErrorOr<string>> UploadFileAsync(IFormFile file, string keyPrefix, bool useKeyPrefixAsFullUrl = false, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required and cannot be empty.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return Error.Validation("INVALID_FILE_TYPE", $"File type '{extension}' is not allowed.");

        if (extension == ".jpeg") extension = ".jpg";

        var maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return Error.Validation("INVALID_FILE_SIZE", $"File size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB.");

        string key;
        if (useKeyPrefixAsFullUrl)
        {
            key = $"{keyPrefix}{extension}".TrimStart('/');
        }
        else
        {
            var fileName = $"{Guid.NewGuid()}{extension}";
            key = string.IsNullOrEmpty(keyPrefix) ? fileName : $"{keyPrefix}/{fileName}".TrimStart('/');
        }

        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(filePath) ?? _basePath;
        Directory.CreateDirectory(dir);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        // Return the stored key (relative path within bucket)
        return key;
    }

    public Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath)) File.Delete(filePath);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> GetPreSignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        // Map to the static files endpoint path
        var url = $"/files/{_options.BucketName}/{key}".Replace("\\", "/").Replace("//", "/");
        return Task.FromResult(url);
    }
}
