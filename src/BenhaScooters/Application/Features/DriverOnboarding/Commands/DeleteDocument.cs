using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record DeleteDocumentCommand(
    DriverId DriverId,
    DocumentType DocumentType) : IRequest<ErrorOr<Deleted>>;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, ErrorOr<Deleted>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3;

    public DeleteDocumentCommandHandler(AppDbContext db, IS3Service s3)
    {
        _db = db;
        _s3 = s3;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var result = driver.RemoveDocument(request.DocumentType);
        if (result.IsError) return result.Errors;

        await _db.SaveChangesAsync(cancellationToken);

        await _s3.DeleteFileAsync(result.Value, cancellationToken);

        return Result.Deleted;
    }
}