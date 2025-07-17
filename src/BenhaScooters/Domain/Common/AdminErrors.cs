using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class AdminErrors
{
    public static Error DriverNotFound => Error.NotFound(
        "ADMIN_DRIVER_NOT_FOUND",
        "Driver not found.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "The specified driver does not exist in the system."}
        });

    public static Error ValidationError(string message) => Error.Validation(
        "ADMIN_VALIDATION_ERROR",
        message);

    public static Error InvalidOperation(string message) => Error.Validation(
        "ADMIN_INVALID_OPERATION",
        message,
        metadata: new Dictionary<string, object>
        {
            {"Detail", "The requested operation cannot be performed in the current state."}
        });
}
