using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Driver document (owned by DriverProfile)
/// </summary>
public class DriverDocument
{
    public int Id { get; private set; }  // Auto-generated
    public UserId DriverUserId { get; private set; }
    public DocumentType Type { get; private set; }
    public string ImageUrl { get; private set; } = null!;
    public DateTime UploadedAt { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }

    private DriverDocument() { } // For EF Core

    public DriverDocument(UserId driverUserId, DocumentType type, string imageUrl, DateOnly? expiryDate = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL is required.", nameof(imageUrl));

        DriverUserId = driverUserId;
        Type = type;
        ImageUrl = imageUrl;
        UploadedAt = DateTime.UtcNow;
        ExpiryDate = expiryDate;
    }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
}
