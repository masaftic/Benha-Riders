using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using NetTopologySuite.Geometries;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Vehicle Identification Number - 17 character code
/// </summary>
[ValueObject<string>]
public partial struct VIN
{
    private static Validation Validate(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return Validation.Invalid("VIN cannot be empty.");

        // Basic VIN validation (17 characters for modern vehicles)
        if (vin.Length != 17)
            return Validation.Invalid("VIN must be 17 characters.");

        return Validation.Ok;
    }
}



public enum DriverOnboardingStatus
{
    Incomplete = 0,     // Still filling out profile
    UnderReview = 1,    // Submitted, awaiting admin review
    Approved = 2,       // Can go online and accept trips
    Rejected = 3,       // Needs to fix issues and resubmit
    Suspended = 4       // Was approved but now suspended
}

/// <summary>
/// Driver profile containing static/rarely-updated driver information.
/// Uses UserId as primary key (1:1 relationship with User).
/// </summary>
public class DriverProfile
{
    public UserId UserId { get; private set; }  // PK & FK to User
    
    // Personal information (embedded value object)
    public DriverPersonalInfo? PersonalInfo { get; private set; }
    
    // Vehicle information (embedded value object)
    public DriverVehicleInfo? Vehicle { get; private set; }
    
    // Documents (owned collection)
    private readonly List<DriverDocument> _documents = [];
    public IReadOnlyList<DriverDocument> Documents => _documents.AsReadOnly();
    
    // Onboarding status
    public DriverOnboardingStatus OnboardingStatus { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public User User { get; private set; } = null!;

    private DriverProfile() { } // For EF Core

    public DriverProfile(UserId userId)
    {
        UserId = userId;
        OnboardingStatus = DriverOnboardingStatus.Incomplete;
        CreatedAt = DateTime.UtcNow;
    }

    public ErrorOr<Success> UpdatePersonalInfo(DriverPersonalInfo personalInfo)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return DriverErrors.Profile.CannotModifyApprovedProfile;

        PersonalInfo = personalInfo;
        return Result.Success;
    }

    public ErrorOr<Success> UpdateVehicle(DriverVehicleInfo vehicle)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return DriverErrors.Profile.CannotModifyApprovedProfile;

        Vehicle = vehicle;
        return Result.Success;
    }

    public ErrorOr<Success> AddDocument(DocumentType type, string imageUrl, DateOnly? expiryDate = null)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return DriverErrors.Profile.CannotModifyApprovedProfile;

        // Remove existing document of same type
        var existing = _documents.FirstOrDefault(d => d.Type == type);
        if (existing != null)
            _documents.Remove(existing);

        _documents.Add(new DriverDocument(UserId, type, imageUrl, expiryDate));
        return Result.Success;
    }

    public ErrorOr<string> RemoveDocument(DocumentType type)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return DriverErrors.Profile.CannotModifyApprovedProfile;

        var document = _documents.FirstOrDefault(d => d.Type == type);
        if (document == null)
            return DriverErrors.Document.NotFound;

        var imageUrl = document.ImageUrl;
        _documents.Remove(document);
        return imageUrl;
    }

    public ErrorOr<Success> SubmitForReview()
    {
        if (OnboardingStatus != DriverOnboardingStatus.Incomplete && 
            OnboardingStatus != DriverOnboardingStatus.Rejected)
            return DriverErrors.Profile.InvalidStatusTransition;

        if (PersonalInfo == null)
            return DriverErrors.Profile.PersonalInfoRequired;

        if (Vehicle == null)
            return DriverErrors.Profile.VehicleInfoRequired;

        var requiredDocs = GetRequiredDocumentTypes();
        var uploadedTypes = _documents.Select(d => d.Type).ToHashSet();
        if (!requiredDocs.All(uploadedTypes.Contains))
            return DriverErrors.Profile.DocumentsRequired;

        OnboardingStatus = DriverOnboardingStatus.UnderReview;
        RejectionReason = null;
        return Result.Success;
    }

    public ErrorOr<Success> Approve(UserId adminId)
    {
        if (OnboardingStatus != DriverOnboardingStatus.UnderReview)
            return DriverErrors.Profile.InvalidStatusTransition;

        OnboardingStatus = DriverOnboardingStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = adminId;
        RejectionReason = null;
        return Result.Success;
    }

    public ErrorOr<Success> Reject(string reason, UserId? rejectedBy = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DriverErrors.Profile.RejectionReasonRequired;

        if (OnboardingStatus != DriverOnboardingStatus.UnderReview)
            return DriverErrors.Profile.InvalidStatusTransition;

        OnboardingStatus = DriverOnboardingStatus.Rejected;
        RejectionReason = reason;
        if (rejectedBy != null)
            ApprovedBy = rejectedBy;  // Reuse ApprovedBy field for audit
        return Result.Success;
    }

    public ErrorOr<Success> Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DriverErrors.Profile.SuspensionReasonRequired;

        if (OnboardingStatus != DriverOnboardingStatus.Approved)
            return DriverErrors.Profile.InvalidStatusTransition;

        OnboardingStatus = DriverOnboardingStatus.Suspended;
        RejectionReason = reason;  // Reuse for suspension reason
        return Result.Success;
    }

    public ErrorOr<Success> Reinstate()
    {
        if (OnboardingStatus != DriverOnboardingStatus.Suspended)
            return DriverErrors.Profile.InvalidStatusTransition;

        OnboardingStatus = DriverOnboardingStatus.Approved;
        RejectionReason = null;
        return Result.Success;
    }

    public ErrorOr<Success> Unsuspend(UserId adminId)
    {
        if (OnboardingStatus != DriverOnboardingStatus.Suspended)
            return DriverErrors.Profile.InvalidStatusTransition;

        OnboardingStatus = DriverOnboardingStatus.Approved;
        RejectionReason = null;
        ApprovedBy = adminId;
        return Result.Success;
    }

    public bool CanGoOnline => OnboardingStatus == DriverOnboardingStatus.Approved;
    
    public bool IsComplete => PersonalInfo != null && Vehicle != null && 
                              GetRequiredDocumentTypes().All(t => _documents.Any(d => d.Type == t));

    public static List<DocumentType> GetRequiredDocumentTypes() =>
    [
        DocumentType.DrivingLicense,
        DocumentType.VehicleRegistration,
        DocumentType.DriverPhoto
    ];
}

/// <summary>
/// Value object for driver personal information
/// </summary>
public class DriverPersonalInfo : ValueObject
{
    public string FullName { get; private set; } = null!;
    public NationalId NationalId { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public PhoneNumber EmergencyContactPhone { get; private set; }

    private DriverPersonalInfo() { } // For EF Core

    public DriverPersonalInfo(
        string fullName, 
        NationalId nationalId, 
        DateOnly dateOfBirth,
        string address, 
        string city, 
        string emergencyContactName, 
        PhoneNumber emergencyContactPhone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(emergencyContactName))
            throw new ArgumentException("Emergency contact name is required.", nameof(emergencyContactName));

        FullName = fullName;
        NationalId = nationalId;
        DateOfBirth = dateOfBirth;
        Address = address;
        City = city;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullName;
        yield return NationalId;
        yield return DateOfBirth;
        yield return Address;
        yield return City;
        yield return EmergencyContactName;
        yield return EmergencyContactPhone;
    }
}

/// <summary>
/// Value object for driver vehicle information
/// </summary>
public class DriverVehicleInfo : ValueObject
{
    public VehicleType VehicleType { get; private set; }
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public LicensePlate LicensePlate { get; private set; }
    public int Year { get; private set; }
    public VIN VIN { get; private set; }

    private DriverVehicleInfo() { } // For EF Core

    public DriverVehicleInfo(
        VehicleType vehicleType,
        string brand,
        string model,
        string color,
        LicensePlate licensePlate,
        int year,
        VIN vin)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Vehicle brand is required.", nameof(brand));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Vehicle model is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Vehicle color is required.", nameof(color));
        if (year < 1980 || year > DateTime.Now.Year + 1)
            throw new ArgumentException("Invalid vehicle year.", nameof(year));

        VehicleType = vehicleType;
        Brand = brand;
        Model = model;
        Color = color;
        LicensePlate = licensePlate;
        Year = year;
        VIN = vin;
    }

    public string DisplayName => $"{Brand} {Model} ({Year})";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VehicleType;
        yield return Brand;
        yield return Model;
        yield return Color;
        yield return LicensePlate;
        yield return Year;
        yield return VIN;
    }
}

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

public enum DocumentType
{
    DrivingLicense,
    VehicleRegistration,
    DriverPhoto
}
