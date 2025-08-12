using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.Events;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverId;


public class Driver : AggregateRoot
{
    public DriverId Id { get; private set; }
    public UserId UserId { get; private set; }

    // Core driver information
    public DriverInfo? Info { get; private set; }

    // Onboarding state
    public OnboardingState OnboardingState { get; private set; }
    private readonly List<DriverField> _fields = [];
    public IReadOnlyList<DriverField> Fields => _fields.AsReadOnly();

    // Driver status
    public bool IsActive { get; private set; }

    // Child entities within the aggregate

    public DriverVehicle? Vehicle { get; private set; }
    private readonly List<DriverDocument> _documents = new();
    public IReadOnlyList<DriverDocument> Documents => _documents.AsReadOnly();

    // Navigation
    public User User { get; private set; } = null!;

    private Driver() // For EF Core
    {
        OnboardingState = null!; // Will be set by EF Core
    }

    public Driver(UserId userId)
    {
        UserId = userId;
        OnboardingState = OnboardingState.CreateNew();
        IsActive = false;
    }

    public ErrorOr<Success> UpdatePersonalInfo(string fullName, NationalId nationalId, DateOnly dateOfBirth,
        string address, string city, string emergencyContactName, PhoneNumber emergencyContactPhone)
    {
        if (OnboardingState.IsCompleted)
            return DriverErrors.OnboardingAlreadyCompleted;

        if (!OnboardingState.CanAdvanceFrom(OnboardingStatus.PersonalInfo))
            return DriverErrors.PreviousStepsRequired;

        Info = new DriverInfo(fullName, nationalId, dateOfBirth, address, city, emergencyContactName, emergencyContactPhone);
        OnboardingState = OnboardingState.AdvanceToNextStep();

        // Clear existing personal info fields and recreate them with values
        _fields.RemoveAll(f => f.Step == OnboardingStatus.PersonalInfo.ToString());

        var personalInfoFields = new[]
        {
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.FullName)),
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.NationalId)),
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.DateOfBirth)),
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.Address)),
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.City)),
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.EmergencyContactName)),
            new DriverField(Id, OnboardingStatus.PersonalInfo.ToString(), nameof(DriverInfo.EmergencyContactPhone))
        };

        _fields.AddRange(personalInfoFields);

        return Result.Success;
    }

    public ErrorOr<Success> EnrollVehicle(VehicleType vehicleType, string vehicleBrand, string vehicleModel,
        string vehicleColor, LicensePlate licensePlate, int vehicleYear, VIN vin)
    {
        if (OnboardingState.IsCompleted)
            return DriverErrors.OnboardingAlreadyCompleted;

        if (!OnboardingState.CanAdvanceFrom(OnboardingStatus.VehicleInfo))
            return DriverErrors.PersonalInfoRequired;

        Vehicle = new DriverVehicle(Id, vehicleType, vehicleBrand, vehicleModel, vehicleColor, licensePlate, vehicleYear, vin);

        OnboardingState = OnboardingState.AdvanceToNextStep();

        _fields.RemoveAll(f => f.Step == OnboardingStatus.VehicleInfo.ToString());

        var vehicleInfoFields = new[]
        {
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.VehicleType)),
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.Brand)),
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.Model)),
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.Color)),
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.LicensePlate)),
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.Year)),
            new DriverField(Id, OnboardingStatus.VehicleInfo.ToString(), nameof(DriverVehicle.VIN))
        };

        _fields.AddRange(vehicleInfoFields);

        return Result.Success;
    }


    public ErrorOr<Success> AddDocument(DocumentType documentType, string imageUrl, DateOnly? expiryDate = null)
    {
        if (OnboardingState.IsCompleted)
            return DriverErrors.OnboardingAlreadyCompleted;

        // Remove existing document of same type
        var existingDoc = _documents.FirstOrDefault(d => d.Type == documentType); // TODO: delete existing image from blob storage
        if (existingDoc != null)
        {
            _documents.Remove(existingDoc);
        }

        var document = new DriverDocument(Id, documentType, imageUrl, expiryDate);
        _documents.Add(document);

        _fields.RemoveAll(f => f.Step == OnboardingStatus.Documents.ToString() && f.FieldName == documentType.ToString());
        _fields.Add(new DriverField(Id, OnboardingStatus.Documents.ToString(), documentType.ToString()));

        // Check if all required documents are uploaded
        var requiredDocs = new[] { DocumentType.DrivingLicense, DocumentType.VehicleRegistration, DocumentType.DriverPhoto };
        var hasAllDocs = requiredDocs.All(type => _documents.Any(d => d.Type == type));

        if (hasAllDocs)
        {
            // If driver was rejected and uploaded all the documents
            if (OnboardingState.Status == OnboardingStatus.Rejected && CanTransitionToReview())
            {
                GoToReview();
            }
            // driver is doing the normal flow
            else
            {
                OnboardingState = OnboardingState.AdvanceToNextStep();
            }
        }

        return Result.Success;
    }


    public ErrorOr<Success> ApproveEntireStep(string step)
    {
        // Approve all fields in the step
        var fieldsToApprove = _fields.Where(f => f.Step == step && f.Status == FieldStatus.Pending).ToList();
        if (fieldsToApprove.Count == 0)
        {
            return DriverErrors.NoPendingFieldsForStep;
        }

        foreach (var field in fieldsToApprove)
        {
            var result = field.Approve();
            if (result.IsError)
                return result.Errors;
        }

        return Result.Success;
    }

    public ErrorOr<Success> ApproveField(string step, string fieldName)
    {
        var field = _fields.FirstOrDefault(f => f.Step == step && f.FieldName.ToLower() == fieldName.ToLower());
        if (field == null)
            return DriverErrors.FieldNotFound;

        var result = field.Approve();
        if (result.IsError)
            return result.Errors;


        return Result.Success;
    }

    public ErrorOr<Success> RejectField(string step, string fieldName, string reason)
    {
        var field = _fields.FirstOrDefault(f => f.Step == step && f.FieldName.ToLower() == fieldName.ToLower());
        if (field == null)
            return DriverErrors.FieldNotFound;

        var result = field.Reject(reason);
        if (result.IsError)
            return result.Errors;

        if (OnboardingState.Status != OnboardingStatus.Rejected)
        {
            OnboardingState = OnboardingState.Reject();
        }

        return Result.Success;
    }

    public ErrorOr<Success> PatchField(string step, string fieldName, string value)
    {
        ErrorOr<Success> result;

        if (step == OnboardingStatus.PersonalInfo.ToString())
        {
            if (Info == null)
                return DriverErrors.PersonalInfoRequired;

            result = Info.UpdateField(fieldName, value);
        }
        else if (step == OnboardingStatus.VehicleInfo.ToString())
        {
            if (Vehicle == null)
                return DriverErrors.VehicleInfoRequired;

            result = Vehicle.UpdateField(fieldName, value);
        }
        else
        {
            return Error.Validation("UNKNOWN_ONBOARDING_STEP", $"Unknown onboarding step: {step}");
        }

        if (result.IsError) return result.Errors;

        _fields.RemoveAll(f => f.Step == step && f.FieldName.ToLower() == fieldName.ToLower());
        _fields.Add(new DriverField(Id, step, fieldName));

        if (OnboardingState.Status == OnboardingStatus.Rejected && CanTransitionToReview())
        {
            GoToReview();
        }

        return Result.Success;
    }

    public static List<string> GetRequiredDocuments()
    {
        return [
            DocumentType.DrivingLicense.ToString(),
            DocumentType.VehicleRegistration.ToString(),
            DocumentType.DriverPhoto.ToString()
        ];
    }



    /// <summary>
    /// Removes a document from the driver's profile.
    /// </summary>
    /// <param name="documentType"></param>
    /// <returns>Image url for the removed document</returns>
    public ErrorOr<string> RemoveDocument(DocumentType documentType)
    {
        if (OnboardingState.IsCompleted)
            return DriverErrors.OnboardingAlreadyCompleted;

        var document = _documents.FirstOrDefault(d => d.Type == documentType);
        if (document == null)
            return DriverErrors.DocumentNotFound;

        _documents.Remove(document);
        return document.ImageUrl;
    }

    public ErrorOr<Success> CompleteOnboarding()
    {
        if (!OnboardingState.CanComplete)
            return DriverErrors.OnboardingIncomplete;

        // Validate all required data is present
        if (Info == null)
            return DriverErrors.PersonalInfoRequired;

        if (Vehicle is null)
            return DriverErrors.VehicleInfoRequired;

        var requiredDocs = new[] { DocumentType.DrivingLicense, DocumentType.VehicleRegistration, DocumentType.DriverPhoto };
        var missingDocs = requiredDocs.Any(d => !_documents.Any(doc => doc.Type == d));

        if (missingDocs)
            return DriverErrors.DocumentsRequired;

        // Check if all fields are approved
        var unapprovedFields = _fields.Where(f => f.Status != FieldStatus.Approved).ToList();
        if (unapprovedFields.Count != 0)
            return DriverErrors.FieldsNotApproved;

        OnboardingState = OnboardingState.Complete();
        IsActive = true;

        RaiseDomainEvent(new DriverOnboardingCompletedEvent(Id, OnboardingState.CompletedAt!.Value));

        return Result.Success;
    }

    public ErrorOr<Success> BanDriver(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DriverErrors.RejectionReasonRequired;

        OnboardingState = OnboardingState.Ban(reason);
        IsActive = false;

        return Result.Success;
    }

    public bool CanTransitionToReview()
    {
        return _fields.All(f => f.Status != FieldStatus.Rejected);
    }

    public void GoToReview()
    {
        if (!CanTransitionToReview())
            throw new InvalidOperationException("Driver cannot transition to review.");

        OnboardingState = OnboardingState.MoveToReview();
    }

    public int OnboardingProgress => OnboardingState.ProgressPercentage;
}
