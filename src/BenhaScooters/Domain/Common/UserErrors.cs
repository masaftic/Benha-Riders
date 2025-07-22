using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class UserErrors
{
    public static Error EmailAlreadyExists => Error.Conflict(
        "USER_EMAIL_ALREADY_EXISTS", 
        "البريد الإلكتروني مستخدم بالفعل.");

    public static Error PhoneAlreadyExists => Error.Conflict(
        "USER_PHONE_ALREADY_EXISTS", 
        "رقم الهاتف مستخدم بالفعل.");

    public static Error InvalidCredentials => Error.Validation(
        "INVALID_CREDENTIALS", 
        "رقم الهاتف أو كلمة المرور غير صحيحة.");

    public static Error UserNotFound => Error.NotFound(
        "USER_NOT_FOUND", 
        "المستخدم غير موجود.");

    public static Error PhoneAlreadyVerified => Error.Validation(
        "PHONE_ALREADY_VERIFIED", 
        "رقم الهاتف تم التحقق منه بالفعل.");

    public static Error InvalidVerificationCode => Error.Validation(
        "INVALID_VERIFICATION_CODE", 
        "رمز التحقق غير صحيح.");

    public static Error VerificationCodeExpired => Error.Validation(
        "VERIFICATION_CODE_EXPIRED", 
        "انتهت صلاحية رمز التحقق.");

    public static Error VerificationCodeAlreadyUsed => Error.Validation(
        "VERIFICATION_CODE_ALREADY_USED", 
        "تم استخدام رمز التحقق بالفعل.");

    public static Error TooManyVerificationRequests => Error.Validation(
        "TOO_MANY_VERIFICATION_REQUESTS", 
        "يرجى الانتظار قبل طلب رمز تحقق آخر.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "يمكنك طلب رمز تحقق جديد بعد دقيقة واحدة."}
        });

    public static Error IncorrectCurrentPassword => Error.Validation(
        "INCORRECT_CURRENT_PASSWORD", 
        "كلمة المرور الحالية غير صحيحة.");

    public static Error InvalidRefreshToken => Error.Unauthorized(
        "INVALID_REFRESH_TOKEN", 
        "رمز التحديث غير صحيح أو منتهي الصلاحية.");

    public static Error PhoneNumberNotVerified => Error.Forbidden(
        "PHONE_NUMBER_NOT_VERIFIED", 
        "يجب التحقق من رقم الهاتف قبل اختيار الدور.");

    public static Error RoleAlreadyAssigned => Error.Conflict(
        "ROLE_ALREADY_ASSIGNED", 
        "المستخدم لديه دور مخصص بالفعل.");

    public static Error InvalidGoogleIdToken => Error.Validation(
        "INVALID_GOOGLE_ID_TOKEN", 
        "رمز Google ID غير صالح.");
}
