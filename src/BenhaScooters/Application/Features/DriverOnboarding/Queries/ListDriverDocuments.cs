using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record ListDriverDocuments(DriverId DriverId) : IRequest<ErrorOr<ListDriverDocumentsResponse>>;

public record ListDriverDocumentsResponse(
    List<string> RequiredDocuments,
    List<string> MissingDocuments,
    List<DocumentDto> Documents);

public class ListDriverDocumentsHandler(AppDbContext db, IS3Service s3) : IRequestHandler<ListDriverDocuments, ErrorOr<ListDriverDocumentsResponse>>
{
    private readonly AppDbContext _db = db;
    private readonly IS3Service _s3 = s3;

    public async Task<ErrorOr<ListDriverDocumentsResponse>> Handle(ListDriverDocuments request, CancellationToken cancellationToken)
    {
        var driverDocuments = await _db.Drivers
            .Where(d => d.Id == request.DriverId)
            .Select(d => d.Documents)
            .FirstOrDefaultAsync(cancellationToken);

        if (driverDocuments == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var documentTasks = driverDocuments.Select(x => x.ToDto(_s3));
        List<DocumentDto> documents = [.. await Task.WhenAll(documentTasks)];

        var requiredDocuments = Driver.GetRequiredDocuments();
        var missingDocuments = requiredDocuments.Except(documents.Select(d => d.Type)).ToList();

        return new ListDriverDocumentsResponse(
            RequiredDocuments: requiredDocuments,
            MissingDocuments: missingDocuments,
            Documents: documents
        );
    }
}
