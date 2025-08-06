using BenhaScooters.Application.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UploadDocumentCommand(
    DriverId DriverId,
    DocumentType DocumentType,
    IFormFile File,
    DateOnly? ExpiryDate = null) : IRequest<ErrorOr<Success>>;

public class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public UploadDocumentHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<Success>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken: cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var uploadResult = await _s3Service.UploadFileAsync(request.File, $"drivers/{driver.Id}/documents/{request.DocumentType.ToKebabCase()}", useKeyPrefixAsFullUrl: true, cancellationToken);
        if (uploadResult.IsError)
        {
            return uploadResult.Errors;
        }

        var imageUrl = uploadResult.Value;

        var result = driver.AddDocument(request.DocumentType, imageUrl, request.ExpiryDate);
        if (result.IsError)
        {
            await _s3Service.DeleteFileAsync(imageUrl, cancellationToken);
            return result.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
