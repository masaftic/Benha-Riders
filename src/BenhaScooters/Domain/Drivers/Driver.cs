using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.Events;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Vogen;
using static BenhaScooters.Domain.Drivers.Entities.DocumentType;

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
    
    // Driver status
    public bool IsActive { get; private set; }

    // Child entities within the aggregate
    private readonly List<DriverDocument> _documents = new();
    private readonly List<DriverVehicle> _vehicles = new();
    
    public IReadOnlyList<DriverDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyList<DriverVehicle> Vehicles => _vehicles.AsReadOnly();

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

        if (!OnboardingState.CanAdvanceFrom(OnboardingStep.PersonalInfo))
            return DriverErrors.PreviousStepsRequired;

        Info = new DriverInfo(fullName, nationalId, dateOfBirth, address, city, emergencyContactName, emergencyContactPhone);
        OnboardingState = OnboardingState.AdvanceToNextStep();

        return Result.Success;
    }

    public ErrorOr<Success> AddVehicle(VehicleType vehicleType, string vehicleBrand, string vehicleModel, 
        string vehicleColor, LicensePlate licensePlate, int vehicleYear, VIN vin)
    {
        if (OnboardingState.IsCompleted)
            return DriverErrors.OnboardingAlreadyCompleted;

        if (!OnboardingState.CanAdvanceFrom(OnboardingStep.VehicleInfo))
            return DriverErrors.PersonalInfoRequired;

        // Deactivate existing vehicles
        foreach (var vehicle in _vehicles)
        {
            vehicle.Deactivate();
        }

        var newVehicle = new DriverVehicle(Id, vehicleType, vehicleBrand, vehicleModel, vehicleColor, licensePlate, vehicleYear, vin);
        _vehicles.Add(newVehicle);
        
        OnboardingState = OnboardingState.AdvanceToNextStep();

        return Result.Success;
    }

    public ErrorOr<Success> AddDocument(DocumentType documentType, string imageUrl, DateTime? expiryDate = null)
    {
        if (OnboardingState.IsCompleted)
            return DriverErrors.OnboardingAlreadyCompleted;

        if (!OnboardingState.CanAdvanceFrom(OnboardingStep.Documents))
            return DriverErrors.PreviousStepsRequired;

        // Remove existing document of same type
        var existingDoc = _documents.FirstOrDefault(d => d.Type == documentType);
        if (existingDoc != null)
        {
            _documents.Remove(existingDoc);
        }

        var document = new DriverDocument(Id, documentType, imageUrl, expiryDate);
        _documents.Add(document);
        ApproveDocument(documentType); // Automatically approve new document

        // Check if all required documents are uploaded
        var requiredDocs = new[] { DocumentType.DrivingLicense, DocumentType.VehicleRegistration, DocumentType.DriverPhoto };
        var hasAllDocs = requiredDocs.All(type => _documents.Any(d => d.Type == type));

        if (hasAllDocs)
        {
            OnboardingState = OnboardingState.AdvanceToNextStep();
        }

        return Result.Success;
    }

    public ErrorOr<Success> CompleteOnboarding()
    {
        if (!OnboardingState.CanComplete)
            return DriverErrors.OnboardingIncomplete;

        // Validate all required data is present
        if (Info == null)
            return DriverErrors.PersonalInfoRequired;

        if (!_vehicles.Any(v => v.IsActive))
            return DriverErrors.VehicleInfoRequired;

        var requiredDocs = new[] { DocumentType.DrivingLicense, DocumentType.VehicleRegistration, DocumentType.DriverPhoto };
        var missingDocs = requiredDocs.Where(type => !_documents.Any(d => d.Type == type && d.IsValid)).ToList();
        
        if (missingDocs.Any())
            return DriverErrors.DocumentsRequired;

        OnboardingState = OnboardingState.Complete();
        IsActive = true;

        RaiseDomainEvent(new DriverOnboardingCompletedEvent(Id, OnboardingState.CompletedAt!.Value));

        return Result.Success;
    }

    public ErrorOr<Success> RejectOnboarding(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DriverErrors.RejectionReasonRequired;

        OnboardingState = OnboardingState.Reject(reason);
        IsActive = false;

        return Result.Success;
    }

    public ErrorOr<Success> ApproveDocument(DocumentType documentType)
    {
        var document = _documents.FirstOrDefault(d => d.Type == documentType);
        if (document == null)
            return DriverErrors.DocumentNotFound;

        document.Approve();
        return Result.Success;
    }

    public ErrorOr<Success> RejectDocument(DocumentType documentType, string reason)
    {
        var document = _documents.FirstOrDefault(d => d.Type == documentType);
        if (document == null)
            return DriverErrors.DocumentNotFound;

        document.Reject(reason);
        return Result.Success;
    }

    // Properties for compatibility and convenience
    public bool IsOnboardingComplete => OnboardingState.IsCompleted;
    public bool CanGoOnline => IsOnboardingComplete && IsActive && AllDocumentsValid;
    public int OnboardingProgress => OnboardingState.ProgressPercentage;
    public DriverVehicle? ActiveVehicle => _vehicles.FirstOrDefault(v => v.IsActive);
    
    private bool AllDocumentsValid => _documents.All(d => d.IsValid);
}
