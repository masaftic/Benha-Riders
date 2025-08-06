using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record ApproveDocumentCommand(
    DriverId DriverId,
    DocumentType DocumentType,
    DateOnly? ExpiryDate) : IRequest<ErrorOr<Success>>;

public class ApproveDocumentCommandHandler : IRequestHandler<ApproveDocumentCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public ApproveDocumentCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(ApproveDocumentCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var result = driver.ApproveDocument(request.DocumentType, request.ExpiryDate);

        if (result.IsError) return result.Errors;
        
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}

