using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Common;

public static partial class AppErrors
{
    public static class User
    {
        public static AppError AlreadyExists(Email email) => NewConflict(
            "USER_ALREADY_EXISTS",
            $"A user with email '{email}' already exists.",
            ("email", email.ToString()));

        public static AppError PhoneAlreadyExists(PhoneNumber phoneNumber) => NewConflict(
            "USER_PHONE_ALREADY_EXISTS",
            $"A user with phone number '{phoneNumber}' already exists.",
            ("phoneNumber", phoneNumber.ToString()));

        public static AppError InvalidCredentials() => NewValidation(
            "INVALID_CREDENTIALS",
            "The provided credentials are invalid.");

        public static AppError NotFound() => NewNotFound(
            "USER_NOT_FOUND",
            "User was not found.");

        public static AppError PhoneAlreadyVerified() => NewValidation(
            "PHONE_ALREADY_VERIFIED",
            "Phone number is already verified.");

        public static AppError InvalidVerificationCode() => NewValidation(
            "INVALID_VERIFICATION_CODE",
            "The verification code is invalid.");

        public static AppError OtpRateLimitExceeded(int retryAfterSeconds) => NewCustom(
            429,
            "OTP_RATE_LIMIT_EXCEEDED",
            "OTP request rate limit exceeded.",
            ("retryAfterSeconds", retryAfterSeconds));

        public static AppError OtpDailyLimitExceeded() => NewCustom(
            429,
            "OTP_DAILY_LIMIT_EXCEEDED",
            "OTP daily limit exceeded.");

        public static AppError OtpCodeLocked() => NewValidation(
            "OTP_CODE_LOCKED",
            "The verification code is locked due to too many failed attempts.");

        public static AppError OtpVerificationLocked(int retryAfterSeconds) => NewCustom(
            429,
            "OTP_VERIFICATION_LOCKED",
            "OTP verification is temporarily locked.",
            ("retryAfterSeconds", retryAfterSeconds));

        public static AppError OtpFraudSuspected() => NewCustom(
            403,
            "OTP_FRAUD_SUSPECTED",
            "Suspicious OTP activity was detected.");

        public static AppError IncorrectCurrentPassword() => NewValidation(
            "INCORRECT_CURRENT_PASSWORD",
            "The current password is incorrect.");

        public static AppError InvalidRefreshToken() => NewUnauthorized(
            "INVALID_REFRESH_TOKEN",
            "The refresh token is invalid or expired.");

        public static AppError InvalidGoogleIdToken() => NewValidation(
            "INVALID_GOOGLE_ID_TOKEN",
            "The Google ID token is invalid.");

        public static AppError AlreadyHasRole() => NewConflict(
            "USER_ALREADY_HAS_ROLE",
            "User already has this role.");

        public static AppError InvalidDevicePlatform() => NewValidation(
            "INVALID_PLATFORM",
            "Platform must be either 'android' or 'ios'.");
    }
}
