using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.ValueObjects;

namespace BenhaScooters.Domain.Drivers.Events;

public record DriverOnboardingStepCompletedEvent(
    DriverId DriverId,
    OnboardingStep CompletedStep,
    OnboardingStep NextStep) : DomainEvent;
