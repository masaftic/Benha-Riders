using ErrorOr;

namespace BenhaScooters.Domain.Common;

public static class UserErrors
{
    public static Error EmailAlreadyExists => Error.Conflict(
        "USER_EMAIL_ALREADY_EXISTS", 
        "Email is already in use.");

    public static Error PhoneAlreadyExists => Error.Conflict(
        "USER_PHONE_ALREADY_EXISTS", 
        "Phone number is already in use.");

    public static Error InvalidCredentials => Error.Validation(
        "INVALID_CREDENTIALS", 
        "Invalid email or password.");

    public static Error PhoneNotVerified => Error.Forbidden(
        "PHONE_NOT_VERIFIED", 
        "Phone number must be verified before login.");

    public static Error UserNotFound => Error.NotFound(
        "USER_NOT_FOUND", 
        "User not found.");

    public static Error PhoneAlreadyVerified => Error.Validation(
        "PHONE_ALREADY_VERIFIED", 
        "Phone number is already verified.");

    public static Error InvalidVerificationCode => Error.Validation(
        "INVALID_VERIFICATION_CODE", 
        "Invalid verification code.");

    public static Error VerificationCodeExpired => Error.Validation(
        "VERIFICATION_CODE_EXPIRED", 
        "Verification code has expired.");

    public static Error VerificationCodeAlreadyUsed => Error.Validation(
        "VERIFICATION_CODE_ALREADY_USED", 
        "Verification code has already been used.");

    public static Error TooManyVerificationRequests => Error.Validation(
        "TOO_MANY_VERIFICATION_REQUESTS", 
        "Please wait before requesting another verification code.",
        metadata: new Dictionary<string, object>
        {
            {"Detail", "You can request a new verification code after 1 minute."}
        });

    public static Error IncorrectCurrentPassword => Error.Validation(
        "INCORRECT_CURRENT_PASSWORD", 
        "Current password is incorrect.");

    public static Error InvalidRefreshToken => Error.Unauthorized(
        "INVALID_REFRESH_TOKEN", 
        "Invalid or expired refresh token.");

    public static Error PhoneNumberNotVerified => Error.Forbidden(
        "PHONE_NUMBER_NOT_VERIFIED", 
        "Phone number must be verified before selecting a role.");

    public static Error RoleAlreadyAssigned => Error.Conflict(
        "ROLE_ALREADY_ASSIGNED", 
        "User already has a role assigned.");
}
