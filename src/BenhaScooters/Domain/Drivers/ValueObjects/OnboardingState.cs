using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using Vogen;

namespace BenhaScooters.Domain.Drivers.ValueObjects;

public enum OnboardingStep
{
    PersonalInfo = 1,
    VehicleInfo = 2,
    Documents = 3,
    Review = 4,
    Completed = 5
}

public enum OnboardingStatus
{
    NotStarted,
    InProgress,
    Completed,
    Rejected
}

public class OnboardingState : ValueObject
{
    public OnboardingStatus Status { get; private set; }
    public OnboardingStep CurrentStep { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private OnboardingState() { } // For EF Core

    public static OnboardingState CreateNew()
    {
        return new OnboardingState
        {
            Status = OnboardingStatus.NotStarted,
            CurrentStep = OnboardingStep.PersonalInfo,
            CreatedAt = DateTime.UtcNow
        };
    }

    public OnboardingState AdvanceToNextStep()
    {
        var nextStep = CurrentStep switch
        {
            OnboardingStep.PersonalInfo => OnboardingStep.VehicleInfo,
            OnboardingStep.VehicleInfo => OnboardingStep.Documents,
            OnboardingStep.Documents => OnboardingStep.Review,
            OnboardingStep.Review => OnboardingStep.Completed,
            OnboardingStep.Completed => OnboardingStep.Completed,
            _ => throw new InvalidOperationException($"Invalid onboarding step: {CurrentStep}")
        };

        var newStatus = Status == OnboardingStatus.NotStarted ? OnboardingStatus.InProgress : Status;

        return new OnboardingState
        {
            Status = newStatus,
            CurrentStep = nextStep,
            CreatedAt = CreatedAt,
            CompletedAt = CompletedAt,
            RejectionReason = RejectionReason
        };
    }

    public OnboardingState Complete()
    {
        return new OnboardingState
        {
            Status = OnboardingStatus.Completed,
            CurrentStep = OnboardingStep.Completed,
            CreatedAt = CreatedAt,
            CompletedAt = DateTime.UtcNow,
            RejectionReason = null
        };
    }


    public OnboardingState MoveBackToDocuments()
    {
        if (Status == OnboardingStatus.Completed)
            throw new InvalidOperationException("Cannot move back from completed onboarding");

        return new OnboardingState
        {
            Status = OnboardingStatus.InProgress,
            CurrentStep = OnboardingStep.Documents,
            CreatedAt = CreatedAt,
            CompletedAt = null,
            RejectionReason = null
        };
    }

    public OnboardingState Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        return new OnboardingState
        {
            Status = OnboardingStatus.Rejected,
            CurrentStep = CurrentStep,
            CreatedAt = CreatedAt,
            CompletedAt = null,
            RejectionReason = reason
        };
    }

    public bool IsCompleted => Status == OnboardingStatus.Completed;
    public bool CanAdvanceFrom(OnboardingStep step) => CurrentStep == step;
    public bool CanComplete => CurrentStep == OnboardingStep.Review;
    public int ProgressPercentage => (int)CurrentStep * 20; // 20% per step

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Status;
        yield return CurrentStep;
        yield return CreatedAt;
        yield return CompletedAt ?? DateTime.MinValue;
        yield return RejectionReason ?? string.Empty;
    }
}
