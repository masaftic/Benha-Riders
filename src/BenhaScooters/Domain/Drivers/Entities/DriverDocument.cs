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

public enum DocumentStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}

public class DriverDocument
{
    public DriverDocumentId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public DocumentType Type { get; private set; }
    public string ImageUrl { get; private set; } = null!;
    public DocumentStatus Status { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string? RejectionReason { get; private set; }

    // Navigation property
    public Driver Driver { get; private set; } = null!;

    private DriverDocument() { } // For EF Core

    public DriverDocument(DriverId driverId, DocumentType type, string imageUrl, DateTime? expiryDate = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.", nameof(imageUrl));

        DriverId = driverId;
        Type = type;
        ImageUrl = imageUrl;
        Status = DocumentStatus.Pending;
        UploadedAt = DateTime.UtcNow;
        ExpiryDate = expiryDate;
    }

    public void Approve()
    {
        if (Status == DocumentStatus.Approved)
            return;

        Status = DocumentStatus.Approved;
        RejectionReason = null;
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = DocumentStatus.Rejected;
        RejectionReason = reason;
    }

    public void CheckExpiry()
    {
        if (ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow && Status == DocumentStatus.Approved)
        {
            Status = DocumentStatus.Expired;
        }
    }

    internal async Task<DocumentDto?> ToDto(IS3Service s3)
    {
        return new DocumentDto(
            Type.ToString(),
            Status.ToString(),
            await s3.GetPreSignedUrlAsync(ImageUrl, TimeSpan.FromMinutes(15)),
            UploadedAt,
            ExpiryDate,
            RejectionReason,
            IsValid,
            IsExpired);
    }

    public bool IsValid => Status == DocumentStatus.Approved && 
                          (!ExpiryDate.HasValue || ExpiryDate.Value > DateTime.UtcNow);
    
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;
}
