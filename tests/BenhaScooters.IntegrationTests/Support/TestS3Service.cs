using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace BenhaScooters.IntegrationTests.Support;

public sealed class TestS3Service : IS3Service
{
    public Task<ErrorOr<string>> UploadFileAsync(
        IFormFile file,
        string keyPrefix,
        bool useKeyPrefixAsFullUrl = false,
        CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrWhiteSpace(keyPrefix)
            ? file.FileName
            : $"{keyPrefix.TrimEnd('/')}/{file.FileName}";

        return Task.FromResult<ErrorOr<string>>(key);
    }

    public Task<bool> DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<string> GetPreSignedUrlAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://test.local/files/{key.TrimStart('/')}");
    }
}
