using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using NetTopologySuite.Geometries;
using Thinktecture;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Vehicle Identification Number - 17 character code
/// </summary>

[ValueObject<string>]
[KeyMemberEqualityComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
[KeyMemberComparer<ComparerAccessors.StringOrdinalIgnoreCase, string>]
public partial class VIN
{
    static partial void ValidateFactoryArguments(
        ref ValidationError? validationError,
        ref string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationError = new ValidationError("الرقم التسلسلي للمركبة (VIN) مطلوب.");
            return;
        }

        if (value.Length != 17)
        {
            validationError = new ValidationError("يجب أن يكون الرقم التسلسلي للمركبة (VIN) مكونًا من 17 حرفًا.");
            return;
        }
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

    public DriverProfile(User user)
    {
        User = user;
        UserId = user.Id;
        OnboardingStatus = DriverOnboardingStatus.Incomplete;
        CreatedAt = DateTime.UtcNow;
    }


    public ErrorOr<Success> UpdatePersonalInfo(DriverPersonalInfo personalInfo)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return AppErrors.Driver.Profile.CannotModifyApprovedProfile();

        PersonalInfo = personalInfo;
        return Result.Success;
    }

    public ErrorOr<Success> UpdateVehicle(DriverVehicleInfo vehicle)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return AppErrors.Driver.Profile.CannotModifyApprovedProfile();

        Vehicle = vehicle;
        return Result.Success;
    }

    public ErrorOr<Success> AddDocument(DocumentType type, string imageUrl, DateOnly? expiryDate = null)
    {
        if (OnboardingStatus == DriverOnboardingStatus.Approved)
            return AppErrors.Driver.Profile.CannotModifyApprovedProfile();

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
            return AppErrors.Driver.Profile.CannotModifyApprovedProfile();

        var document = _documents.FirstOrDefault(d => d.Type == type);
        if (document == null)
            return AppErrors.Driver.Document.NotFound();

        var imageUrl = document.ImageUrl;
        _documents.Remove(document);
        return imageUrl;
    }

    public ErrorOr<Success> SubmitForReview()
    {
        if (OnboardingStatus != DriverOnboardingStatus.Incomplete && 
            OnboardingStatus != DriverOnboardingStatus.Rejected)
            return AppErrors.Driver.Profile.InvalidStatusTransition();

        if (PersonalInfo == null)
            return AppErrors.Driver.Profile.PersonalInfoRequired();

        if (Vehicle == null)
            return AppErrors.Driver.Profile.VehicleInfoRequired();

        var requiredDocs = GetRequiredDocumentTypes();
        var uploadedTypes = _documents.Select(d => d.Type).ToHashSet();
        if (!requiredDocs.All(uploadedTypes.Contains))
            return AppErrors.Driver.Profile.DocumentsRequired();

        OnboardingStatus = DriverOnboardingStatus.UnderReview;
        RejectionReason = null;
        return Result.Success;
    }

    public ErrorOr<Success> Approve(UserId adminId)
    {
        if (OnboardingStatus != DriverOnboardingStatus.UnderReview)
            return AppErrors.Driver.Profile.InvalidStatusTransition();

        OnboardingStatus = DriverOnboardingStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = adminId;
        RejectionReason = null;
        return Result.Success;
    }

    public ErrorOr<Success> Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return AppErrors.Driver.Profile.RejectionReasonRequired();

        if (OnboardingStatus != DriverOnboardingStatus.UnderReview)
            return AppErrors.Driver.Profile.InvalidStatusTransition();

        OnboardingStatus = DriverOnboardingStatus.Rejected;
        RejectionReason = reason;
        return Result.Success;
    }

    public ErrorOr<Success> Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return AppErrors.Driver.Profile.SuspensionReasonRequired();

        if (OnboardingStatus != DriverOnboardingStatus.Approved)
            return AppErrors.Driver.Profile.InvalidStatusTransition();

        OnboardingStatus = DriverOnboardingStatus.Suspended;
        RejectionReason = reason;  // Reuse for suspension reason
        return Result.Success;
    }

    public ErrorOr<Success> Reinstate()
    {
        if (OnboardingStatus != DriverOnboardingStatus.Suspended)
            return AppErrors.Driver.Profile.InvalidStatusTransition();

        OnboardingStatus = DriverOnboardingStatus.Approved;
        RejectionReason = null;
        return Result.Success;
    }

    public bool CanGoOnline => OnboardingStatus == DriverOnboardingStatus.Approved;
    
    public bool IsComplete => PersonalInfo != null && Vehicle != null && 
                              GetRequiredDocumentTypes().All(t => _documents.Any(d => d.Type == t));

    public void Anonymize(UserId userId)
    {
        PersonalInfo = new DriverPersonalInfo(
            "Deleted Driver",
            NationalId.Create(userId.ToString("D14"))); // Fake national ID;

        Vehicle = new DriverVehicleInfo(
            Vehicle?.VehicleType ?? VehicleType.Scooter,
            "Deleted",
            "Vehicle",
            "Hidden",
            LicensePlate.Create("DELETED"),
            DateTime.UtcNow.Year);

        _documents.Clear();
        OnboardingStatus = DriverOnboardingStatus.Suspended;
        RejectionReason = "Account deleted by user.";
    }

    public static List<DocumentType> GetRequiredDocumentTypes() =>
    [
        DocumentType.DrivingLicense,
        DocumentType.VehicleRegistration,
        DocumentType.DriverPhoto
    ];
}

public enum DocumentType
{
    DrivingLicense,
    VehicleRegistration,
    DriverPhoto,
}
