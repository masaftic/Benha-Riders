using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record ListDriverDocuments(UserId DriverId) : IRequest<ErrorOr<ListDriverDocumentsResponse>>;

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
        var userId = request.DriverId;
        
        var driverDocuments = await _db.DriverProfiles
            .Where(d => d.UserId == userId)
            .Select(d => d.Documents)
            .FirstOrDefaultAsync(cancellationToken);

        if (driverDocuments == null)
        {
            return DriverErrors.Profile.NotFound;
        }

        var documentTasks = driverDocuments.Select(async doc => new DocumentDto(
            Type: doc.Type.ToString(),
            ImageUrl: await _s3.GetPreSignedUrlAsync(doc.ImageUrl, TimeSpan.FromMinutes(15), cancellationToken),
            UploadedAt: doc.UploadedAt,
            ExpiryDate: doc.ExpiryDate
        ));
        List<DocumentDto> documents = [.. await Task.WhenAll(documentTasks)];

        var requiredDocuments = DriverProfile.GetRequiredDocumentTypes().Select(t => t.ToString()).ToList();
        var missingDocuments = requiredDocuments.Except(documents.Select(d => d.Type)).ToList();

        return new ListDriverDocumentsResponse(
            RequiredDocuments: requiredDocuments,
            MissingDocuments: missingDocuments,
            Documents: documents
        );
    }
}
