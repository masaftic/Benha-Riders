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

    public static Error FieldNotFound => Error.Validation(
        "FIELD_NOT_FOUND",
        "الحقل المطلوب غير موجود.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "الحقل الذي تحاول الوصول إليه غير موجود في بيانات السائق."}
        });

    public static Error FieldsNotApproved => Error.Validation(
        "FIELDS_NOT_APPROVED",
        "بعض الحقول لم تتم الموافقة عليها بعد.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يجب الموافقة على جميع الحقول قبل إكمال التسجيل."}
        });

    public static Error NoPendingFieldsForStep => Error.Validation(
        "NO_PENDING_FIELDS_FOR_STEP",
        "لا توجد حقول معلقة للموافقة في هذه الخطوة.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "جميع الحقول في هذه الخطوة تمت مراجعتها بالفعل."}
        });

    public static Error FieldAlreadyReviewed = Error.Validation(
        "FIELD_ALREADY_REVIEWED",
        "تم مراجعة هذا الحقل بالفعل.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "لا يمكن الموافقة على حقل تمت مراجعته مسبقاً."}
        });

    // Driver availability errors
    public static class Availability
    {
        public static readonly Error InvalidStatus = Error.Conflict(
            "DRIVER_INVALID_STATUS",
            "لا يمكن تنفيذ هذه العملية مع حالة السائق الحالية");
    }

    // Driver profile errors (new simplified onboarding)
    public static class Profile
    {
        public static readonly Error CannotModifyApprovedProfile = Error.Conflict(
            "CANNOT_MODIFY_APPROVED_PROFILE",
            "لا يمكن تعديل الملف الشخصي بعد الموافقة عليه.");

        public static readonly Error InvalidStatusTransition = Error.Conflict(
            "INVALID_STATUS_TRANSITION",
            "لا يمكن الانتقال إلى هذه الحالة من الحالة الحالية.");

        public static readonly Error PersonalInfoRequired = Error.Validation(
            "PERSONAL_INFO_REQUIRED",
            "يجب إكمال المعلومات الشخصية.");

        public static readonly Error VehicleInfoRequired = Error.Validation(
            "VEHICLE_INFO_REQUIRED",
            "يجب إكمال معلومات المركبة.");

        public static readonly Error DocumentsRequired = Error.Validation(
            "DOCUMENTS_REQUIRED",
            "يجب رفع جميع المستندات المطلوبة.");

        public static readonly Error RejectionReasonRequired = Error.Validation(
            "REJECTION_REASON_REQUIRED",
            "يجب تقديم سبب للرفض.");

        public static readonly Error SuspensionReasonRequired = Error.Validation(
            "SUSPENSION_REASON_REQUIRED",
            "يجب تقديم سبب للإيقاف.");

        public static readonly Error NotFound = Error.NotFound(
            "DRIVER_PROFILE_NOT_FOUND",
            "الملف الشخصي للسائق غير موجود.");
    }

    // Driver document errors
    public static class Document
    {
        public static readonly Error NotFound = Error.NotFound(
            "DOCUMENT_NOT_FOUND",
            "المستند غير موجود.");

        public static readonly Error InvalidType = Error.Validation(
            "INVALID_DOCUMENT_TYPE",
            "نوع المستند غير صالح.");
    }

    // Driver status errors (online/offline/trip)
    public static class Status
    {
        public static readonly Error CannotGoOnlineWhileOnTrip = Error.Conflict(
            "CANNOT_GO_ONLINE_WHILE_ON_TRIP",
            "لا يمكن التحول لوضع الاتصال أثناء الرحلة.");

        public static readonly Error CannotGoOfflineWhileOnTrip = Error.Conflict(
            "CANNOT_GO_OFFLINE_WHILE_ON_TRIP",
            "لا يمكن قطع الاتصال أثناء الرحلة.");

        public static readonly Error MustBeOnlineToStartTrip = Error.Conflict(
            "MUST_BE_ONLINE_TO_START_TRIP",
            "يجب أن تكون متصلاً لبدء رحلة.");

        public static readonly Error NotOnTrip = Error.Conflict(
            "NOT_ON_TRIP",
            "أنت لست في رحلة حالياً.");

        public static readonly Error MustBeOnlineToSetBusy = Error.Conflict(
            "MUST_BE_ONLINE_TO_SET_BUSY",
            "يجب أن تكون متصلاً لتعيين حالة مشغول.");

        public static readonly Error NotFound = Error.NotFound(
            "DRIVER_STATUS_NOT_FOUND",
            "حالة السائق غير موجودة.");
    }

    // Driver stats errors
    public static class Stats
    {
        public static readonly Error NotFound = Error.NotFound(
            "DRIVER_STATS_NOT_FOUND",
            "إحصائيات السائق غير موجودة.");
    }

    // Driver location errors
    public static class Location
    {
        public static readonly Error NotFound = Error.NotFound(
            "DRIVER_LOCATION_NOT_FOUND",
            "موقع السائق غير موجود.");

        public static readonly Error Stale = Error.Conflict(
            "DRIVER_LOCATION_STALE",
            "موقع السائق قديم جداً.");
    }
}

public static class WalletErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "WALLET_NOT_FOUND",
        "محفظة السائق غير موجودة.");

    public static readonly Error DebtLimitExceeded = Error.Conflict(
        "WALLET_DEBT_LIMIT_EXCEEDED",
        "لا يمكن قبول الرحلة. تجاوز الدين الحد المسموح به. يرجى تسوية المستحقات أولاً.");

    public static readonly Error InvalidAmount = Error.Validation(
        "WALLET_INVALID_AMOUNT",
        "المبلغ غير صالح.");

    public static readonly Error AdjustmentReasonRequired = Error.Validation(
        "WALLET_ADJUSTMENT_REASON_REQUIRED",
        "يجب تقديم سبب للتعديل.");

    public static Error InsufficientBalance(decimal required, decimal available) => Error.Conflict(
        "WALLET_INSUFFICIENT_BALANCE",
        $"الرصيد غير كافٍ. المطلوب: {required}, المتاح: {available}");
}
