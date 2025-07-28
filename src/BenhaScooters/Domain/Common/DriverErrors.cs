using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class DriverErrors
{
    public static Error DriverNotFound => Error.NotFound(
        "DRIVER_NOT_FOUND",
        "السائق غير موجود. يرجى الحصول على حالة التسجيل أولاً.");

    public static Error DuplicateNationalId => Error.Conflict(
        "DUPLICATE_NATIONAL_ID",
        "يوجد سائق آخر بنفس رقم الهوية الوطنية.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "رقم الهوية الوطنية يجب أن يكون فريداً لكل سائق."}
        });


    public static Error UploadError(string message) => Error.Failure(
        "DOCUMENT_UPLOAD_ERROR",
        $"فشل في رفع المستندات: {message}",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يرجى المحاولة مرة أخرى. تأكد من أن الملفات صور صحيحة وأقل من 10 ميجابايت."}
        });

    // Domain-specific onboarding errors
    public static Error OnboardingAlreadyCompleted => Error.Validation(
        "ONBOARDING_ALREADY_COMPLETED",
        "لا يمكن تحديث المعلومات بعد اكتمال التسجيل.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "بمجرد اكتمال التسجيل، لا يمكن تعديل معلومات السائق."}
        });

    public static Error PersonalInfoRequired => Error.Validation(
        "PERSONAL_INFO_REQUIRED",
        "يجب إكمال المعلومات الشخصية أولاً.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب إكمال المعلومات الشخصية قبل الانتقال إلى معلومات المركبة."}
        });

    public static Error PreviousStepsRequired => Error.Validation(
        "PREVIOUS_STEPS_REQUIRED",
        "يجب إكمال الخطوات السابقة أولاً.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب إكمال جميع خطوات التسجيل السابقة قبل رفع المستندات."}
        });

    public static Error OnboardingIncomplete => Error.Validation(
        "ONBOARDING_INCOMPLETE",
        "يجب إكمال جميع خطوات التسجيل أولاً.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب تقديم المعلومات الشخصية ومعلومات المركبة والمستندات قبل الموافقة."}
        });

    public static Error RejectionReasonRequired => Error.Validation(
        "REJECTION_REASON_REQUIRED",
        "سبب الرفض مطلوب.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب تقديم سبب واضح للرفض لمساعدة السائق على فهم ما يحتاج إلى تصحيح."}
        });

    public static Error DeactivationReasonRequired => Error.Validation(
        "DEACTIVATION_REASON_REQUIRED",
        "سبب إلغاء التفعيل مطلوب.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب تقديم سبب واضح لإلغاء تفعيل السائق."}
        });

    public static Error VehicleInfoRequired => Error.Validation(
        "VEHICLE_INFO_REQUIRED",
        "معلومات المركبة مطلوبة.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب إضافة مركبة صالحة قبل إكمال التسجيل."}
        });

    public static Error DocumentsRequired => Error.Validation(
        "DOCUMENTS_REQUIRED",
        "المستندات المطلوبة غير مكتملة.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب رفع جميع المستندات المطلوبة والموافقة عليها قبل إكمال التسجيل."}
        });

    public static Error DocumentNotFound => Error.NotFound(
        "DOCUMENT_NOT_FOUND",
        "المستند غير موجود.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "المستند المطلوب غير موجود أو تم حذفه."}
        });

    public static Error InvalidFileFormat => Error.Validation(
        "INVALID_FILE_FORMAT",
        "تنسيق الملف غير صحيح. يُسمح فقط بملفات JPG و JPEG و PNG.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يرجى رفع الصور بتنسيق JPG أو JPEG أو PNG بحد أقصى 10 ميجابايت."}
        });

    public static Error FileTooLarge => Error.Validation(
        "FILE_TOO_LARGE",
        "حجم الملف يتجاوز الحد الأقصى المسموح.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "الحد الأقصى المسموح لحجم الملف هو 10 ميجابايت."}
        });

    // Driver availability errors
    public static class Availability
    {
        public static readonly Error InvalidStatus = Error.Conflict(
            "DRIVER_INVALID_STATUS",
            "لا يمكن تنفيذ هذه العملية مع حالة السائق الحالية");
    }
}
