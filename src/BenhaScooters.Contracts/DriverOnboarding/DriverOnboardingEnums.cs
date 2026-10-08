namespace BenhaScooters.Contracts.DriverOnboarding;

public enum VehicleType
{
    Motorcycle,
    Scooter,
    Bicycle,
    Car,
    Suzuki
}

public enum DriverOnboardingStatus
{
    Incomplete = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3,
    Suspended = 4
}

public enum DocumentType
{
    DrivingLicense,
    VehicleRegistration,
    DriverPhoto
}
