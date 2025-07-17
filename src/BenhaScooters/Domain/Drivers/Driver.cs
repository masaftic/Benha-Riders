using System.Text.RegularExpressions;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverId;


public class Driver
{
    public DriverId Id { get; private set; }
    public UserId UserId { get; private set; }
    
    public PersonalInfo? PersonalInfo { get; private set; }
    public VehicleInfo? VehicleInfo { get; private set; }
    public DriverDocuments? Documents { get; private set; }
    
    // Onboarding Status
    public OnboardingStatus OnboardingStatus { get; private set; }
    public OnboardingStep CurrentStep { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    
    // Driver Status
    public bool IsActive { get; private set; }

    // Navigation Properties
    public DriverRating Rating { get; private set; } = null!;


    private Driver() { } // For EF Core

    public Driver(UserId userId)
    {
        UserId = userId;
        OnboardingStatus = OnboardingStatus.NotStarted;
        CurrentStep = OnboardingStep.PersonalInfo;
        CreatedAt = DateTime.UtcNow;
        IsActive = false;
    }

    public ErrorOr<Success> UpdatePersonalInfo(string fullName, NationalId nationalId, DateOnly dateOfBirth, 
        string address, string city, string emergencyContactName, PhoneNumber emergencyContactPhone)
    {
        if (OnboardingStatus == OnboardingStatus.Completed)
            return DriverErrors.OnboardingAlreadyCompleted;

        PersonalInfo = new PersonalInfo(fullName, nationalId, dateOfBirth, address, city, emergencyContactName, emergencyContactPhone);

        if (OnboardingStatus == OnboardingStatus.NotStarted)
            OnboardingStatus = OnboardingStatus.InProgress;

        if (CurrentStep == OnboardingStep.PersonalInfo)
            CurrentStep = OnboardingStep.VehicleInfo;

        return Result.Success;
    }

    public ErrorOr<Success> UpdateVehicleInfo(VehicleType vehicleType, string vehicleBrand, string vehicleModel, 
        string vehicleColor, LicensePlate licensePlate, int vehicleYear)
    {
        if (OnboardingStatus == OnboardingStatus.Completed)
            return DriverErrors.OnboardingAlreadyCompleted;

        if (CurrentStep < OnboardingStep.VehicleInfo)
            return DriverErrors.PersonalInfoRequired;

        VehicleInfo = new VehicleInfo(vehicleType, vehicleBrand, vehicleModel, vehicleColor, licensePlate, vehicleYear);

        if (CurrentStep == OnboardingStep.VehicleInfo)
            CurrentStep = OnboardingStep.Documents;

        return Result.Success;
    }

    public ErrorOr<Success> UpdateDocuments(string licenseImageUrl, string vehicleRegistrationImageUrl, string ImageUrl)
    {
        if (OnboardingStatus == OnboardingStatus.Completed)
            return DriverErrors.OnboardingAlreadyCompleted;

        if (CurrentStep < OnboardingStep.Documents)
            return DriverErrors.PreviousStepsRequired;

        Documents = new DriverDocuments(licenseImageUrl, vehicleRegistrationImageUrl, ImageUrl);

        if (CurrentStep == OnboardingStep.Documents)
            CurrentStep = OnboardingStep.Review;

        return Result.Success;
    }

    public ErrorOr<Success> CompleteOnboarding()
    {
        if (CurrentStep != OnboardingStep.Review)
            return DriverErrors.OnboardingIncomplete;

        OnboardingStatus = OnboardingStatus.Completed;
        CurrentStep = OnboardingStep.Completed;
        CompletedAt = DateTime.UtcNow;
        IsActive = true;
        RejectionReason = null;

        return Result.Success;
    }

    public ErrorOr<Success> RejectOnboarding(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DriverErrors.RejectionReasonRequired;

        OnboardingStatus = OnboardingStatus.Rejected;
        RejectionReason = reason;
        IsActive = false;

        return Result.Success;
    }

    public void AddRating(decimal newRating)
    {
        Rating.AddRating(newRating);
    }

    public bool IsOnboardingComplete => OnboardingStatus == OnboardingStatus.Completed;
    public bool CanGoOnline => IsOnboardingComplete && IsActive;
    public int OnboardingProgress => (int)CurrentStep * 20; // 20% per step
}
