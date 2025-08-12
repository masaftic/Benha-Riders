using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Infrastructure.S3;
using Vogen;

namespace BenhaScooters.Domain.Drivers.Entities;

[ValueObject<int>]
public partial struct DriverDocumentId;

public enum DocumentType
{
    DrivingLicense,
    VehicleRegistration,
    DriverPhoto
}


public class DriverDocument
{
    public DriverDocumentId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public DocumentType Type { get; private set; }
    public string ImageUrl { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }


    // Navigation property
    public Driver Driver { get; private set; } = null!;

    private DriverDocument() { } // For EF Core

    public DriverDocument(DriverId driverId, DocumentType type, string imageUrl, DateOnly? expiryDate = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.", nameof(imageUrl));

        DriverId = driverId;
        Type = type;
        ImageUrl = imageUrl;
        UploadedAt = DateTime.UtcNow;
        ExpiryDate = expiryDate;
    }

    internal async Task<DocumentDto> ToDto(IS3Service s3)
    {
        return new DocumentDto(
            Type.ToString(),
            await s3.GetPreSignedUrlAsync(ImageUrl, TimeSpan.FromMinutes(15)),
            UploadedAt,
            ExpiryDate);
    }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
}
