using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class AdminErrors
{
    public static Error DriverNotFound => Error.NotFound(
        "ADMIN_DRIVER_NOT_FOUND",
        "السائق غير موجود.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "السائق المحدد غير موجود في النظام."}
        });

    public static Error DocumentNotFound => Error.NotFound(
        "ADMIN_DOCUMENT_NOT_FOUND",
        "المستند غير موجود.");

    public static Error ValidationError(string message) => Error.Validation(
        "ADMIN_VALIDATION_ERROR",
        message);

    public static Error InvalidOperation(string message) => Error.Validation(
        "ADMIN_INVALID_OPERATION",
        message,
        metadata: new Dictionary<string, object>
        {
            {"Detail", "لا يمكن تنفيذ العملية المطلوبة في الحالة الحالية."}
        });
}
