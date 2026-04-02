using BenhaScooters.Application.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DocumentType = BenhaScooters.Domain.Drivers.DocumentType;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateDocumentsCommand(
    UserId DriverId,
    Dictionary<DocumentType, IFormFile> Documents) : IRequest<ErrorOr<UpdateDocumentsResponse>>;


public record UpdateDocumentsResponse(string Message, DriverOnboardingStatus NextStep);

public class UpdateDocumentsCommandHandler : IRequestHandler<UpdateDocumentsCommand, ErrorOr<UpdateDocumentsResponse>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public UpdateDocumentsCommandHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<UpdateDocumentsResponse>> Handle(UpdateDocumentsCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AppErrors.Driver.Profile.NotFound();
        }

        try
        {
            var driverId = driverProfile.UserId;
            
            // Upload all provided documents and add them to the profile
            foreach (var (documentType, file) in request.Documents)
            {
                var uploadResult = await _s3Service.UploadFileAsync(
                    file,
                    $"drivers/{driverId}/documents/{documentType.ToKebabCase()}",
                    useKeyPrefixAsFullUrl: true,
                    cancellationToken);

                if (uploadResult.IsError) 
                    return uploadResult.Errors;

                var addDocResult = driverProfile.AddDocument(documentType, uploadResult.Value);
                if (addDocResult.IsError)
                    return addDocResult.Errors;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new UpdateDocumentsResponse(
                "Documents uploaded successfully.",
                driverProfile.OnboardingStatus);
        }
        catch (Exception)
        {
            return AppErrors.Driver.UploadFailed();
        }
    }
}
