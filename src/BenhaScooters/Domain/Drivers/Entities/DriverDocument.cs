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
    public DateOnly? ExpiryDate { get; private set; }
    public string? RejectionReason { get; private set; }

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
        Status = DocumentStatus.Pending;
        UploadedAt = DateTime.UtcNow;
        ExpiryDate = expiryDate;
    }

    public void Approve(DateOnly? ExpiryDate)
    {
        if (ExpiryDate.HasValue && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvalidOperationException("Cannot approve a document that has already expired.");
        
        if (Status == DocumentStatus.Approved)
            return;

        if (Status == DocumentStatus.Rejected)
            RejectionReason = null;
        
        if (Status == DocumentStatus.Expired)
            throw new InvalidOperationException("Cannot approve an expired document.");
        
        Status = DocumentStatus.Approved;
    }

    public void UpdateNewImageAfterRejection(string newImageUrl)
    {
        if (string.IsNullOrWhiteSpace(newImageUrl))
            throw new ArgumentException("New image URL is required.", nameof(newImageUrl));

        ImageUrl = newImageUrl;
        Status = DocumentStatus.Pending; // Reset status to pending after rejection
        RejectionReason = null; // Clear previous rejection reason
        UploadedAt = DateTime.UtcNow; // Update upload time
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = DocumentStatus.Rejected;
        RejectionReason = reason;
    }

    internal async Task<DocumentDto> ToDto(IS3Service s3)
    {
        return new DocumentDto(
            Type.ToString(),
            Status.ToString(),
            await s3.GetPreSignedUrlAsync(ImageUrl, TimeSpan.FromMinutes(15)),
            UploadedAt,
            ExpiryDate,
            RejectionReason);
    }

    public bool IsValid => Status == DocumentStatus.Approved && 
                          (!ExpiryDate.HasValue || ExpiryDate.Value > DateOnly.FromDateTime(DateTime.UtcNow));

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
}
