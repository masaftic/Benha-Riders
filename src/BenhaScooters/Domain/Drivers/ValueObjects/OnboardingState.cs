using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using Vogen;

namespace BenhaScooters.Domain.Drivers.ValueObjects;

public enum OnboardingStatus
{
    PersonalInfo = 1,
    VehicleInfo = 2,
    Documents = 3,
    Review = 4,
    Completed = 5,
    Rejected = 0,
    Banned = -1
}

public class OnboardingState : ValueObject
{
    public OnboardingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? BanReason { get; private set; }

    private OnboardingState() { } // For EF Core

    public static OnboardingState CreateNew()
    {
        return new OnboardingState
        {
            Status = OnboardingStatus.PersonalInfo,
            CreatedAt = DateTime.UtcNow
        };
    }

    public OnboardingState AdvanceToNextStep()
    {
        var nextStep = Status switch
        {
            OnboardingStatus.PersonalInfo => OnboardingStatus.VehicleInfo,
            OnboardingStatus.VehicleInfo => OnboardingStatus.Documents,
            OnboardingStatus.Documents => OnboardingStatus.Review,
            OnboardingStatus.Review => OnboardingStatus.Completed,
            OnboardingStatus.Completed => OnboardingStatus.Completed,
            _ => throw new InvalidOperationException($"Invalid onboarding step: {Status}")
        };

        return new OnboardingState
        {
            Status = nextStep,
            CreatedAt = CreatedAt,
            CompletedAt = CompletedAt,
            BanReason = BanReason
        };
    }

    public OnboardingState MoveToReview()
    {
        return new OnboardingState
        {
            Status = OnboardingStatus.Review,
            CreatedAt = CreatedAt,
            CompletedAt = null,
            BanReason = null
        };
    }

    public OnboardingState Reject()
    {
        return new OnboardingState
        {
            Status = OnboardingStatus.Rejected,
            CreatedAt = CreatedAt,
            CompletedAt = null,
        };
    }

    public OnboardingState Complete()
    {
        return new OnboardingState
        {
            Status = OnboardingStatus.Completed,
            CreatedAt = CreatedAt,
            CompletedAt = DateTime.UtcNow,
            BanReason = null
        };
    }


    public OnboardingState MoveBackToDocuments()
    {
        if (Status == OnboardingStatus.Completed)
            throw new InvalidOperationException("Cannot move back from completed onboarding");

        return new OnboardingState
        {
            Status = OnboardingStatus.Documents,
            CreatedAt = CreatedAt,
            CompletedAt = null,
            BanReason = null
        };
    }

    public OnboardingState Ban(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        return new OnboardingState
        {
            Status = OnboardingStatus.Banned,
            CreatedAt = CreatedAt,
            CompletedAt = null,
            BanReason = reason
        };
    }

    public bool IsCompleted => Status == OnboardingStatus.Completed;
    public bool CanAdvanceFrom(OnboardingStatus status) => Status == status; 
    public bool CanComplete => Status == OnboardingStatus.Review;
    public int ProgressPercentage => (int)Status * 20; // 20% per step

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Status;
        yield return CreatedAt;
        yield return CompletedAt ?? DateTime.MinValue;
        yield return BanReason ?? string.Empty;
    }
}
