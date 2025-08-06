using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record RejectDocumentCommand(
    DriverId DriverId,
    DocumentType DocumentType,
    string Reason) : IRequest<ErrorOr<Success>>;

public class RejectDocumentCommandHandler : IRequestHandler<RejectDocumentCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public RejectDocumentCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(RejectDocumentCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var result = driver.RejectDocument(request.DocumentType, request.Reason);
        if (result.IsError) return result.Errors;
        
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
