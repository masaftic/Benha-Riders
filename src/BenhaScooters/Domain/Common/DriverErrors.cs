using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class DriverErrors
{
    public static Error DriverNotFound => Error.NotFound(
        "DRIVER_NOT_FOUND",
        "Driver not found. Please get onboarding status first.");

    public static Error DuplicateNationalId => Error.Conflict(
        "DUPLICATE_NATIONAL_ID",
        "A driver with this national ID already exists.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "National ID must be unique across all drivers."}
        });

    public static Error ValidationError(string message) => Error.Validation(
        "DRIVER_VALIDATION_ERROR",
        message);

    public static Error UploadError(string message) => Error.Failure(
        "DOCUMENT_UPLOAD_ERROR",
        $"Failed to upload documents: {message}",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "Please try uploading the documents again. Ensure files are valid images under 10MB."}
        });

    // Domain-specific onboarding errors
    public static Error OnboardingAlreadyCompleted => Error.Validation(
        "ONBOARDING_ALREADY_COMPLETED",
        "Cannot update information after onboarding is completed.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "Once onboarding is completed, driver information cannot be modified."}
        });

    public static Error PersonalInfoRequired => Error.Validation(
        "PERSONAL_INFO_REQUIRED",
        "Complete personal information first.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "Personal information must be completed before proceeding to vehicle information."}
        });

    public static Error PreviousStepsRequired => Error.Validation(
        "PREVIOUS_STEPS_REQUIRED",
        "Complete previous steps first.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "All previous onboarding steps must be completed before uploading documents."}
        });

    public static Error OnboardingIncomplete => Error.Validation(
        "ONBOARDING_INCOMPLETE",
        "All onboarding steps must be completed first.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "Personal information, vehicle information, and documents must all be submitted before approval."}
        });

    public static Error RejectionReasonRequired => Error.Validation(
        "REJECTION_REASON_REQUIRED",
        "Rejection reason is required.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "A clear rejection reason must be provided to help the driver understand what needs to be corrected."}
        });

    public static Error InvalidFileFormat => Error.Validation(
        "INVALID_FILE_FORMAT",
        "Invalid file format. Only JPG, JPEG, and PNG files are allowed.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "Please upload images in JPG, JPEG, or PNG format with maximum size of 10MB."}
        });

    public static Error FileTooLarge => Error.Validation(
        "FILE_TOO_LARGE",
        "File size exceeds the maximum allowed limit.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "Maximum file size allowed is 10MB."}
        });
}
